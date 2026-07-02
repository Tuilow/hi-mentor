using MediatR;

namespace Tuilow.Finance.Application.Queries.GetPlatformRevenue;

/// <summary>Uso administrativo: receita total retida pela plataforma (comissões) em um período.</summary>
public sealed record GetPlatformRevenueQuery(DateTime? From, DateTime? To) : IRequest<PlatformRevenueResponse>;

public sealed record PlatformRevenueResponse(
    decimal GrossSalesTotal,
    decimal PlatformFeeTotal,
    decimal CreatorsNetTotal,
    int SalesCount
);
