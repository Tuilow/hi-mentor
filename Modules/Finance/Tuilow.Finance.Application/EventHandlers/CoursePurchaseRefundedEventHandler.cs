using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Application.EventHandlers;

/// <summary>
/// Reage ao reembolso de uma compra de curso estornando o valor líquido que havia sido
/// creditado ao criador. Se o lançamento original já havia sido liberado para saque
/// (Available), debita de AvailableBalance; caso contrário, debita de PendingBalance.
/// </summary>
public sealed class CoursePurchaseRefundedEventHandler(
    ICreatorWalletRepository walletRepository,
    IUnitOfWork uow,
    ILogger<CoursePurchaseRefundedEventHandler> logger
) : INotificationHandler<CoursePurchaseRefundedDomainEvent>
{
    public async Task Handle(CoursePurchaseRefundedDomainEvent notification, CancellationToken ct)
    {
        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(notification.CreatorId, ct);
        if (wallet is null)
        {
            logger.LogWarning("Reembolso da compra {PurchaseId} ignorado: carteira do criador {CreatorId} não encontrada.",
                notification.CoursePurchaseId, notification.CreatorId);
            return;
        }

        // Idempotência: um evento de reembolso reentregue (retry de webhook) não deve debitar duas vezes.
        var alreadyRefunded = wallet.Transactions.Any(t =>
            t.ReferenceType == "CoursePurchase" && t.ReferenceId == notification.CoursePurchaseId &&
            t.Type == WalletTransactionType.SaleRefund);
        if (alreadyRefunded)
        {
            logger.LogInformation("Reembolso da compra {PurchaseId} ignorado: já processado anteriormente.",
                notification.CoursePurchaseId);
            return;
        }

        var originalSale = wallet.Transactions.FirstOrDefault(t =>
            t.ReferenceType == "CoursePurchase" && t.ReferenceId == notification.CoursePurchaseId &&
            t.Type == WalletTransactionType.SaleCredit);

        if (originalSale is null)
        {
            // Sem o crédito original não há como saber com segurança o valor líquido nem o balde
            // (Pending/Available) correto a debitar — melhor abortar e logar do que arriscar
            // debitar o valor bruto errado de um balde chutado.
            logger.LogWarning(
                "Reembolso da compra {PurchaseId} ignorado: crédito de venda original não encontrado na carteira do criador {CreatorId}.",
                notification.CoursePurchaseId, notification.CreatorId);
            return;
        }

        var wasAlreadyAvailable = originalSale.Status == WalletTransactionStatus.Available;
        var netToReverse = originalSale.NetAmount;

        var transaction = wallet.RecordRefund(netToReverse, notification.CoursePurchaseId, wasAlreadyAvailable);

        walletRepository.Update(wallet);
        await walletRepository.AddTransactionAsync(transaction, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Reembolso processado: compra {PurchaseId}, valor estornado {Amount} do criador {CreatorId}",
            notification.CoursePurchaseId, netToReverse, notification.CreatorId);
    }
}
