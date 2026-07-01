using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Learning.Events;

public sealed record CourseCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
