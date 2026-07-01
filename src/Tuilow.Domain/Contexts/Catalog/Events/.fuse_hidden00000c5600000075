using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Catalog.Events;

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
