using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Learning.Domain.Events;

public sealed record LessonCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid LessonId, decimal ProgressPercentage
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
