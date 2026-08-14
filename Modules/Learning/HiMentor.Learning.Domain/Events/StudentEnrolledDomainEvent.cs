using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Learning.Domain.Events;

public sealed record StudentEnrolledDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId, string CourseTitle
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
