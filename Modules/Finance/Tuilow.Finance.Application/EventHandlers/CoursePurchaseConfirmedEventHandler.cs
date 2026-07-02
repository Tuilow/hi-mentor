using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Application.EventHandlers;

/// <summary>
/// Reage à confirmação de uma compra de curso (evento publicado pelo módulo Sales) para:
///   1. Consultar o percentual de comissão da plataforma vigente (PlatformFeeConfiguration);
///   2. Calcular comissão (bruto x percentual) e valor líquido do criador;
///   3. Criar a carteira do criador sob demanda, se ainda não existir;
///   4. Registrar o crédito líquido no extrato da carteira (WalletTransaction) e atualizar saldos.
///
/// Este é o único ponto do sistema onde a retenção da comissão Tuilow é calculada — mantém a
/// regra financeira isolada no contexto Finance, sem duplicar lógica em Sales/Catalog/Learning.
/// </summary>
public sealed class CoursePurchaseConfirmedEventHandler(
    ICreatorWalletRepository walletRepository,
    IPlatformFeeConfigurationRepository feeConfigRepository,
    IUnitOfWork uow,
    ILogger<CoursePurchaseConfirmedEventHandler> logger
) : INotificationHandler<CoursePurchaseConfirmedDomainEvent>
{
    /// <summary>Percentual usado como fallback caso nenhuma configuração administrativa tenha sido criada ainda.</summary>
    public const decimal DefaultFeePercentage = 10m;

    public async Task Handle(CoursePurchaseConfirmedDomainEvent notification, CancellationToken ct)
    {
        var feeConfig = await feeConfigRepository.GetActiveAsync(ct);
        var feePercentage = feeConfig?.Percentage ?? DefaultFeePercentage;

        var gross = Money.Of(notification.Amount);
        var feeAmount = Money.Of(Math.Round(gross.Amount * feePercentage / 100m, 2));
        var netAmount = gross.Subtract(feeAmount);

        var wallet = await walletRepository.GetByCreatorIdAsync(notification.CreatorId, ct);
        var isNewWallet = wallet is null;
        wallet ??= CreatorWallet.CreateFor(notification.CreatorId);

        var transaction = wallet.RecordSale(gross, feeAmount, netAmount, feePercentage, notification.CoursePurchaseId);

        if (isNewWallet)
            await walletRepository.AddAsync(wallet, ct);
        else
            walletRepository.Update(wallet);

        await walletRepository.AddTransactionAsync(transaction, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Comissão aplicada: venda {PurchaseId} — bruto {Gross}, taxa {FeePct}% ({Fee}), líquido criador {Net} (creator {CreatorId})",
            notification.CoursePurchaseId, gross, feePercentage, feeAmount, netAmount, notification.CreatorId);
    }
}
