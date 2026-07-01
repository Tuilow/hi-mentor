using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Subscription.Events;

public sealed record PaymentConfirmedDomainEvent(
    Guid SubscriptionId, Guid UserId, string AsaasPaymentId, decimal Amount
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
