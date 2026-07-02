using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetPlatformRevenue;

public sealed class GetPlatformRevenueQueryHandler(ICreatorWalletRepository walletRepository)
    : IRequestHandler<GetPlatformRevenueQuery, PlatformRevenueResponse>
{
    public async Task<PlatformRevenueResponse> Handle(GetPlatformRevenueQuery request, CancellationToken ct)
    {
        var totals = await walletRepository.GetPlatformTotalsAsync(request.From, request.To, ct);
        return new PlatformRevenueResponse(totals.GrossTotal, totals.FeeTotal, totals.NetTotal, totals.SalesCount);
    }
}
