using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.IdentidadeAcesso.Domain.Events;

public sealed record UserEmailConfirmedDomainEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
