using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Events;

public sealed record StudentEnrolledDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId, string CourseTitle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
