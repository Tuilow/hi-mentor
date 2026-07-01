using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Learning.Events;

public sealed record CourseCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
