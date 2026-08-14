using MediatR;

namespace HiMentor.Payout.Application.Commands.RejectPayout;

public sealed record RejectPayoutCommand(Guid PayoutRequestId, Guid AdminUserId, string? Reason) : IRequest;
