using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Learning.Domain.Events;

public sealed record CourseCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid CourseId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
