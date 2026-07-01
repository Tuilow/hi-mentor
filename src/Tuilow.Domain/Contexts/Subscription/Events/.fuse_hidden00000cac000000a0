using DogMaster.Domain.Common.Abstractions;
using DogMaster.Domain.Contexts.Subscription.Enums;

namespace DogMaster.Domain.Contexts.Subscription.Events;

public sealed record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId, Guid UserId, Guid PlanId, BillingCycle BillingCycle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
