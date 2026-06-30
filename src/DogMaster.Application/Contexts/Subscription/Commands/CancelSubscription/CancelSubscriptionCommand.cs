using MediatR;

namespace DogMaster.Application.Contexts.Subscription.Commands.CancelSubscription;

public sealed record CancelSubscriptionCommand(Guid UserId, string? Reason = null) : IRequest;
