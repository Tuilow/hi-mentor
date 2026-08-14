using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Sales.Domain.Enums;

namespace HiMentor.Sales.Domain.Events;

public sealed record SubscriptionCreatedDomainEvent(
    Guid SubscriptionId, Guid UserId, Guid PlanId, BillingCycle BillingCycle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
