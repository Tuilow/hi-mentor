using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Subscription.Events;

public sealed record SubscriptionCancelledDomainEvent(
    Guid SubscriptionId, Guid UserId, string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
