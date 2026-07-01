using MediatR;

namespace Tuilow.Domain.Common.Abstractions;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
