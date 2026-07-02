using MediatR;

namespace Tuilow.Payout.Application.Commands.ApprovePayout;

public sealed record ApprovePayoutCommand(Guid PayoutRequestId, Guid AdminUserId) : IRequest;
