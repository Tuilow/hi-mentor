using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.ValueObjects;
using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Finance.Application.EventHandlers;

/// <summary>
/// Reage à confirmação de uma compra de curso (evento publicado pelo módulo Sales) para creditar
/// a carteira interna do criador -- SOMENTE no modelo Legacy (PaymentModel.Legacy). Numa venda
/// MarketplaceSplit o dinheiro nunca passa pela conta da HiMentor: a cobrança foi criada
/// diretamente na conta Asaas do próprio criador, que já recebeu automaticamente seu valor
/// líquido via split no momento do pagamento -- não há nada para creditar aqui, e tentar
/// creditar mesmo assim duplicaria o valor (o creator receberia duas vezes: uma da Asaas
/// diretamente, outra via saque quinzenal da carteira interna que nunca deveria existir para
/// essa venda).
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
        if (notification.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit)
        {
            logger.LogInformation(
                "Compra {PurchaseId} confirmada no modelo MarketplaceSplit -- nada a creditar na carteira " +
                "interna do criador {CreatorId} (o split da Asaas já entregou o valor líquido diretamente).",
                notification.CoursePurchaseId, notification.CreatorId);
            return;
        }

        // Idempotente: essencial para o reprocessamento manual (achado C2/M1 da auditoria) não
        // creditar a carteira do criador duas vezes para a mesma compra — mesmo padrão de
        // Learning.IsEnrolledAsync (a outra reação a este mesmo evento).
        if (await walletRepository.HasSaleTransactionForPurchaseAsync(notification.CoursePurchaseId, ct))
        {
            logger.LogInformation(
                "Comissão já aplicada anteriormente para a compra {PurchaseId} — nada a fazer.",
                notification.CoursePurchaseId);
            return;
        }

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
