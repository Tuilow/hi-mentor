namespace DogMaster.Domain.Common.Abstractions;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}
