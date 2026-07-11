using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;

public sealed record GetCreatorFinancialDashboardQuery(Guid CreatorId) : IRequest<CreatorFinancialDashboardResponse>;

public sealed record CreatorFinancialDashboardResponse(
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal TotalGrossSales,
    decimal TotalPlatformFeePaid,
    decimal TotalNetEarned,
    decimal TotalWithdrawn,
    int TotalSalesCount,
    DateOnly CurrentCycleStart,
    DateOnly CurrentCycleEnd,
    DateOnly NextReleaseDate
);
