using MediatR;

namespace HiMentor.Finance.Application.Queries.GetCreatorFinancialDashboard;

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
    // Feature 12/08/2026 ("controle de estornos" pedido pelo criador): totais agregados de
    // reembolso, para o dashboard mostrar de cara "quanto foi estornado" sem precisar contar
    // linha a linha na lista de vendas (ver GetCreatorSalesHistoryQuery para o detalhe por venda).
    decimal TotalRefundedAmount,
    int TotalRefundedCount,
    DateOnly CurrentCycleStart,
    DateOnly CurrentCycleEnd,
    DateOnly NextReleaseDate,
    // MarketplaceSplit -- liquida direto na conta Asaas do proprio criador (ver
    // CreatorAsaasAccount); nao existe conceito de "saldo pendente/disponivel" aqui porque a
    // HiMentor nunca fica com o dinheiro do criador em nenhum momento -- estes totais sao so
    // informativos (quanto ja vendeu, quanto de comissao pagou), nao um saldo sacavel.
    decimal MarketplaceGrossSales,
    decimal MarketplaceCommissionPaid,
    decimal MarketplaceNetEarned,
    int MarketplaceSalesCount,
    decimal MarketplaceRefundedAmount,
    int MarketplaceRefundedCount
);
