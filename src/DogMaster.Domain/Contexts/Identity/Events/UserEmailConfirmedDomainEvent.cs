using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Identity.Events;

public sealed record UserEmailConfirmedDomainEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
