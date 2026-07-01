using DogMaster.Domain.Common.Abstractions;
using DogMaster.Domain.Contexts.Catalog.ValueObjects;
using DogMaster.Domain.Contexts.Subscription.Enums;

namespace DogMaster.Domain.Contexts.Subscription.Entities;

public sealed class SubscriptionPayment : Entity
{
    public Guid SubscriptionId { get; private set; }
    public string AsaasPaymentId { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public PaymentMethod? Method { get; private set; }
    public DateOnly DueDate { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private SubscriptionPayment() { }

    public static SubscriptionPayment Create(Guid subscriptionId, string asaasPaymentId,
        decimal amount, DateOnly dueDate, PaymentMethod? method = null) =>
        new()
        {
            SubscriptionId = subscriptionId,
            AsaasPaymentId = asaasPaymentId,
            Amount = Money.Of(amount),
            DueDate = dueDate,
            Method = method
        };

    public void Confirm()
    {
        Status = PaymentStatus.Confirmed;
        PaidAt = DateTime.UtcNow;
        Touch();
    }

    public void Fail() { Status = PaymentStatus.Failed; Touch(); }
    public void Refund() { Status = PaymentStatus.Refunded; Touch(); }
}
