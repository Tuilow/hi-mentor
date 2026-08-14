using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Finance.Application.EventHandlers;

/// <summary>
/// Reage ao reembolso de uma compra de curso estornando o valor líquido que havia sido
/// creditado ao criador -- SOMENTE no modelo Legacy. Numa venda MarketplaceSplit a própria
/// Asaas reverte o split automaticamente quando a cobrança original é estornada/reembolsada
/// (documentação oficial: "se uma cobrança é revertida, o split correspondente também é
/// revertido"), então não existe nenhuma WalletTransaction para debitar aqui.
/// </summary>
public sealed class CoursePurchaseRefundedEventHandler(
    ICreatorWalletRepository walletRepository,
    IUnitOfWork uow,
    ILogger<CoursePurchaseRefundedEventHandler> logger
) : INotificationHandler<CoursePurchaseRefundedDomainEvent>
{
    public async Task Handle(CoursePurchaseRefundedDomainEvent notification, CancellationToken ct)
    {
        if (notification.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit)
        {
            logger.LogInformation(
                "Reembolso da compra {PurchaseId} (MarketplaceSplit) -- nada a estornar na carteira interna " +
                "do criador {CreatorId}; a própria Asaas reverte o split automaticamente.",
                notification.CoursePurchaseId, notification.CreatorId);
            return;
        }

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
