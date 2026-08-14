using MediatR;

namespace HiMentor.Finance.Application.Queries.GetCreatorSalesHistory;

public sealed record GetCreatorSalesHistoryQuery(Guid CreatorId, DateTime? From, DateTime? To) : IRequest<IReadOnlyList<CreatorSaleItemResponse>>;

/// <summary>
/// Um item = uma CoursePurchase (venda avulsa de curso) do criador, em QUALQUER status —
/// Pending/Confirmed/Failed/Refunded — não só as confirmadas. Feature 12/08/2026 (pedido do
/// usuário: "que ele tenha acesso a todo sua parte financeira... quem já comprou e pagou
/// aparecer para ele"): antes, este endpoint só devolvia WalletTransaction (lançamentos da
/// carteira interna), que só existem no modelo Legacy — uma venda MarketplaceSplit (o modelo
/// atual, criador com subconta própria na Asaas) NUNCA gera WalletTransaction, então a lista
/// ficava vazia pra qualquer criador nesse modelo. Agora a fonte é CoursePurchase (cobre os dois
/// modelos igualmente) enriquecida com nome/e-mail do aluno, título do curso, e — só para Legacy —
/// o percentual/valor de comissão realmente aplicado e o status de liberação na carteira (extraídos
/// do WalletTransaction correspondente, que é onde esse dado fica de fato, já que CoursePurchase
/// não grava snapshot de comissão nesse modelo).
/// </summary>
public sealed record CreatorSaleItemResponse(
    Guid CoursePurchaseId,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    Guid CourseId,
    string CourseTitle,
    string PaymentModel,
    string Status,
    decimal GrossAmount,
    /// <summary>Valor retido pela plataforma nesta venda. Nulo se a venda nunca chegou a ser confirmada.</summary>
    decimal? PlatformFeeAmount,
    /// <summary>Percentual de comissão aplicado nesta venda especificamente (pode variar por criador/época).</summary>
    decimal? CommissionPercentage,
    /// <summary>Quanto o criador efetivamente ganhou (líquido) nesta venda.</summary>
    decimal? CreatorNetAmount,
    /// <summary>
    /// Só preenchido no modelo Legacy: "Pending" (ainda no ciclo de 15 dias) ou "Available"
    /// (já liberado para saque). Nulo em MarketplaceSplit — lá o dinheiro nunca passa pela
    /// carteira interna, não existe conceito de "liberação" a controlar.
    /// </summary>
    string? PayoutStatus,
    DateTime? ConfirmedAt,
    DateTime? RefundedAt,
    DateTime CreatedAt
);
