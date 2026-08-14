using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.IdentidadeAcesso.Domain.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string ConfirmationToken
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
