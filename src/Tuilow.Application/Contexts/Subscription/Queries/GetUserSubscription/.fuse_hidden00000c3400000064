using MediatR;

namespace DogMaster.Application.Contexts.Subscription.Queries.GetUserSubscription;

public sealed record GetUserSubscriptionQuery(Guid UserId) : IRequest<UserSubscriptionResponse?>;

public sealed record UserSubscriptionResponse(
    Guid Id,
    string PlanName,
    string Status,
    decimal Price,
    string BillingCycle,
    DateTime CurrentPeriodEnd,
    bool IsActive
);
