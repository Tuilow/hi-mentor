using Tuilow.Finance.Domain.Common;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;

public sealed class GetCreatorFinancialDashboardQueryHandler(
    ICreatorWalletRepository walletRepository,
    ICoursePurchaseRepository coursePurchaseRepository
) : IRequestHandler<GetCreatorFinancialDashboardQuery, CreatorFinancialDashboardResponse>
{
    public async Task<CreatorFinancialDashboardResponse> Handle(
        GetCreatorFinancialDashboardQuery request, CancellationToken ct)
    {
        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(request.CreatorId, ct)
            ?? CreatorWallet.CreateFor(request.CreatorId);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cycle = PayoutCycleCalculator.GetCurrentCycle(today);
        var nextRelease = PayoutCycleCalculator.GetNextReleaseDate(today);

        var salesCount = wallet.Transactions.Count(t => t.Type == WalletTransactionType.SaleCredit);

        // Feature 12/08/2026: totais de estorno, para o KPI "estornos" do dashboard do criador.
        var refundTransactions = wallet.Transactions.Where(t => t.Type == WalletTransactionType.SaleRefund).ToList();

        // Uma única busca de todas as CoursePurchase do criador (sem filtro de data -- este
        // dashboard mostra totais acumulados, não um período), reaproveitada tanto para os totais
        // MarketplaceSplit confirmados quanto para os reembolsados.
        var allPurchases = (await coursePurchaseRepository.GetByCreatorAsync(request.CreatorId, null, null, ct)).ToList();

        // MarketplaceSplit -- somado direto de CoursePurchase (nunca gera WalletTransaction).
        var marketplacePurchases = allPurchases
            .Where(p => p.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit && p.Status == CoursePurchaseStatus.Confirmed)
            .ToList();
        var marketplaceRefunded = allPurchases
            .Where(p => p.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit && p.Status == CoursePurchaseStatus.Refunded)
            .ToList();

        return new CreatorFinancialDashboardResponse(
            wallet.AvailableBalance.Amount,
            wallet.PendingBalance.Amount,
            wallet.TotalGrossSales.Amount,
            wallet.TotalPlatformFeePaid.Amount,
            wallet.TotalNetEarned.Amount,
            wallet.TotalWithdrawn.Amount,
            salesCount,
            TotalRefundedAmount: refundTransactions.Sum(t => t.NetAmount.Amount),
            TotalRefundedCount: refundTransactions.Count,
            cycle.Start,
            cycle.End,
            nextRelease,
            MarketplaceGrossSales: marketplacePurchases.Sum(p => p.Amount.Amount),
            MarketplaceCommissionPaid: marketplacePurchases.Sum(p => p.PlatformCommissionAmount?.Amount ?? 0),
            MarketplaceNetEarned: marketplacePurchases.Sum(p => p.CreatorNetAmount?.Amount ?? 0),
            MarketplaceSalesCount: marketplacePurchases.Count,
            MarketplaceRefundedAmount: marketplaceRefunded.Sum(p => p.Amount.Amount),
            MarketplaceRefundedCount: marketplaceRefunded.Count);
    }
}
