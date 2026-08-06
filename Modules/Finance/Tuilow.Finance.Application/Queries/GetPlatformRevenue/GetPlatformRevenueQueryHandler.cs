using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetPlatformRevenue;

/// <summary>
/// Combina os dois modelos de venda: Legacy (soma vem do extrato da carteira interna,
/// CreatorWallet/WalletTransaction) e MarketplaceSplit (soma vem direto de CoursePurchase, já
/// que essas vendas nunca geram WalletTransaction -- ver CoursePurchaseConfirmedEventHandler).
/// </summary>
public sealed class GetPlatformRevenueQueryHandler(
    ICreatorWalletRepository walletRepository,
    ICoursePurchaseRepository coursePurchaseRepository
) : IRequestHandler<GetPlatformRevenueQuery, PlatformRevenueResponse>
{
    public async Task<PlatformRevenueResponse> Handle(GetPlatformRevenueQuery request, CancellationToken ct)
    {
        var legacy = await walletRepository.GetPlatformTotalsAsync(request.From, request.To, ct);
        var marketplace = await coursePurchaseRepository.GetMarketplaceTotalsAsync(request.From, request.To, ct);

        return new PlatformRevenueResponse(
            GrossSalesTotal: legacy.GrossTotal + marketplace.GrossTotal,
            PlatformFeeTotal: legacy.FeeTotal + marketplace.CommissionTotal,
            CreatorsNetTotal: legacy.NetTotal + marketplace.CreatorNetTotal,
            SalesCount: legacy.SalesCount + marketplace.SalesCount,
            LegacyGrossSalesTotal: legacy.GrossTotal,
            LegacyPlatformFeeTotal: legacy.FeeTotal,
            LegacySalesCount: legacy.SalesCount,
            MarketplaceGrossSalesTotal: marketplace.GrossTotal,
            MarketplacePlatformFeeTotal: marketplace.CommissionTotal,
            MarketplaceSalesCount: marketplace.SalesCount);
    }
}
