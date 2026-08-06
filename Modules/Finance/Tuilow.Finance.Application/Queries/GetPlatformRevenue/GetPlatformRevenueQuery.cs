using MediatR;

namespace Tuilow.Finance.Application.Queries.GetPlatformRevenue;

/// <summary>Uso administrativo: receita total retida pela plataforma (comissões) em um período -- soma Legacy (CreatorWallet) + MarketplaceSplit (CoursePurchase), já que vendas marketplace nunca passam pela carteira interna.</summary>
public sealed record GetPlatformRevenueQuery(DateTime? From, DateTime? To) : IRequest<PlatformRevenueResponse>;

public sealed record PlatformRevenueResponse(
    decimal GrossSalesTotal,
    decimal PlatformFeeTotal,
    decimal CreatorsNetTotal,
    int SalesCount,
    decimal LegacyGrossSalesTotal,
    decimal LegacyPlatformFeeTotal,
    int LegacySalesCount,
    decimal MarketplaceGrossSalesTotal,
    decimal MarketplacePlatformFeeTotal,
    int MarketplaceSalesCount
);
