using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.IdentidadeAcesso.Domain.Events;

public sealed record UserEmailConfirmedDomainEvent(Guid UserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
