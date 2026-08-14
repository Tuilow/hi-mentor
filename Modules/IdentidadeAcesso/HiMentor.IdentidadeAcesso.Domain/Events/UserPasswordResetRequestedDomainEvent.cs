using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.IdentidadeAcesso.Domain.Events;

public sealed record UserPasswordResetRequestedDomainEvent(
    Guid UserId,
    string Email,
    string ResetToken
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
