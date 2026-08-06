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

        // MarketplaceSplit -- somado direto de CoursePurchase (nunca gera WalletTransaction).
        var marketplacePurchases = (await coursePurchaseRepository.GetByCreatorAsync(request.CreatorId, null, null, ct))
            .Where(p => p.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit && p.Status == CoursePurchaseStatus.Confirmed)
            .ToList();

        return new CreatorFinancialDashboardResponse(
            wallet.AvailableBalance.Amount,
            wallet.PendingBalance.Amount,
            wallet.TotalGrossSales.Amount,
            wallet.TotalPlatformFeePaid.Amount,
            wallet.TotalNetEarned.Amount,
            wallet.TotalWithdrawn.Amount,
            salesCount,
            cycle.Start,
            cycle.End,
            nextRelease,
            MarketplaceGrossSales: marketplacePurchases.Sum(p => p.Amount.Amount),
            MarketplaceCommissionPaid: marketplacePurchases.Sum(p => p.PlatformCommissionAmount?.Amount ?? 0),
            MarketplaceNetEarned: marketplacePurchases.Sum(p => p.CreatorNetAmount?.Amount ?? 0),
            MarketplaceSalesCount: marketplacePurchases.Count);
    }
}
