using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Sales.Domain.Events;

public sealed record SubscriptionCancelledDomainEvent(
    Guid SubscriptionId, Guid UserId, string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
