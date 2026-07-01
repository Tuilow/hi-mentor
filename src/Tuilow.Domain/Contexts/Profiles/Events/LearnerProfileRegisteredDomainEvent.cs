using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Profiles.Events;

public sealed record LearnerProfileRegisteredDomainEvent(
    Guid ProfileId, Guid UserId, string Name, string? Category
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
