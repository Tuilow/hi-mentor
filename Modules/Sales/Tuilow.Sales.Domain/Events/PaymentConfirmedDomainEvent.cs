using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Sales.Domain.Events;

public sealed record PaymentConfirmedDomainEvent(
    Guid SubscriptionId, Guid UserId, string AsaasPaymentId, decimal Amount
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
