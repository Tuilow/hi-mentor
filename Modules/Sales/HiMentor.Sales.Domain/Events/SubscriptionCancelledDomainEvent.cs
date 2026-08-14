using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Sales.Domain.Events;

public sealed record SubscriptionCancelledDomainEvent(
    Guid SubscriptionId, Guid UserId, string? Reason
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
