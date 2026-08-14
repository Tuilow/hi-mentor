using MediatR;

namespace HiMentor.Payout.Application.Queries.GetMyPayoutHistory;

public sealed record GetMyPayoutHistoryQuery(Guid CreatorId) : IRequest<IReadOnlyList<PayoutRequestResponse>>;

public sealed record PayoutRequestResponse(
    Guid Id,
    decimal Amount,
    string Status,
    DateOnly CycleStart,
    DateOnly CycleEnd,
    DateTime RequestedAt,
    DateTime? ReviewedAt,
    string? RejectionReason,
    DateTime? PaidAt
);
