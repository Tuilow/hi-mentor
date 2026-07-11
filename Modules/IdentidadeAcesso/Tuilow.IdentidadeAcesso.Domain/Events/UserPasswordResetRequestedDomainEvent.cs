using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.IdentidadeAcesso.Domain.Events;

public sealed record UserPasswordResetRequestedDomainEvent(
    Guid UserId,
    string Email,
    string ResetToken
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
