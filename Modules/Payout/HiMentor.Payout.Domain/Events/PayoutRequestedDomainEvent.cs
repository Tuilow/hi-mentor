using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Payout.Domain.Events;

public sealed record PayoutRequestedDomainEvent(Guid PayoutRequestId, Guid CreatorId, decimal Amount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
