using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Events;

public sealed record LessonCompletedDomainEvent(
    Guid EnrollmentId, Guid UserId, Guid LessonId, decimal ProgressPercentage
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
