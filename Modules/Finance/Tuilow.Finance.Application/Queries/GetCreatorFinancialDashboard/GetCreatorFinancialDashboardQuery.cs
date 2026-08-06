using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;

public sealed record GetCreatorFinancialDashboardQuery(Guid CreatorId) : IRequest<CreatorFinancialDashboardResponse>;

public sealed record CreatorFinancialDashboardResponse(
    // Legacy -- carteira interna (saldo disponivel/pendente, ciclo quinzenal de saque).
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal TotalGrossSales,
    decimal TotalPlatformFeePaid,
    decimal TotalNetEarned,
    decimal TotalWithdrawn,
    int TotalSalesCount,
    DateOnly CurrentCycleStart,
    DateOnly CurrentCycleEnd,
    DateOnly NextReleaseDate,
    // MarketplaceSplit -- liquida direto na conta Asaas do proprio criador (ver
    // CreatorAsaasAccount); nao existe conceito de "saldo pendente/disponivel" aqui porque a
    // Tuilow nunca fica com o dinheiro do criador em nenhum momento -- estes totais sao so
    // informativos (quanto ja vendeu, quanto de comissao pagou), nao um saldo sacavel.
    decimal MarketplaceGrossSales,
    decimal MarketplaceCommissionPaid,
    decimal MarketplaceNetEarned,
    int MarketplaceSalesCount
);
