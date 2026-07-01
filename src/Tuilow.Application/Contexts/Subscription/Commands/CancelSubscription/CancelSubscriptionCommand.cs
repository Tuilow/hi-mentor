using MediatR;

namespace Tuilow.Application.Contexts.Subscription.Commands.CancelSubscription;

public sealed record CancelSubscriptionCommand(Guid UserId, string? Reason = null) : IRequest;
