using MediatR;

namespace Tuilow.Payout.Application.Queries.GetPendingPayoutRequests;

/// <summary>Uso administrativo: lista solicitações de saque aguardando aprovação.</summary>
public sealed record GetPendingPayoutRequestsQuery : IRequest<IReadOnlyList<AdminPayoutRequestResponse>>;

public sealed record AdminPayoutRequestResponse(
    Guid Id,
    Guid CreatorId,
    decimal Amount,
    string Status,
    DateOnly CycleStart,
    DateOnly CycleEnd,
    DateTime RequestedAt
);
