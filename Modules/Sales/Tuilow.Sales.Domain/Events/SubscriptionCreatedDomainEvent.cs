using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Sales.Domain.Enums;

namespace Tuilow.Sales.Domain.Events;

public sealed record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId, Guid UserId, Guid PlanId, BillingCycle BillingCycle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
