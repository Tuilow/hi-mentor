using Tuilow.Finance.Domain.Common;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;

public sealed class GetCreatorFinancialDashboardQueryHandler(ICreatorWalletRepository walletRepository)
    : IRequestHandler<GetCreatorFinancialDashboardQuery, CreatorFinancialDashboardResponse>
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
            nextRelease);
    }
}
