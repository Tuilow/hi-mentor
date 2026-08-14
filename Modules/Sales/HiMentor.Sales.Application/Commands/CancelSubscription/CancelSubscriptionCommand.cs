using MediatR;

namespace HiMentor.Sales.Application.Commands.CancelSubscription;

public sealed record CancelSubscriptionCommand(Guid UserId, string? Reason = null) : IRequest;
