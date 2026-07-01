using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Contexts.Subscription.Enums;

namespace Tuilow.Domain.Contexts.Subscription.Events;

public sealed record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId, Guid UserId, Guid PlanId, BillingCycle BillingCycle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
