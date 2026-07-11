using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Events;

public sealed record CourseCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
