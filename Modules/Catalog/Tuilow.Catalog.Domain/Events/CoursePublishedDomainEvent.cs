using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Catalog.Domain.Events;

public sealed record CoursePublishedDomainEvent(
    Guid CourseId,
    Guid InstructorId,
    string Title,
    string Slug
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
