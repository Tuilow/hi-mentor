using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.DogProfile.Events;

public sealed record DogRegisteredDomainEvent(
    Guid DogId, Guid UserId, string Name, string? Breed
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
