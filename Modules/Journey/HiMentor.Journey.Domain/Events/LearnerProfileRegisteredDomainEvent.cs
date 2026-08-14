using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Journey.Domain.Events;

public sealed record LearnerProfileRegisteredDomainEvent(
    Guid ProfileId, Guid UserId, string Name, string? Category
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
