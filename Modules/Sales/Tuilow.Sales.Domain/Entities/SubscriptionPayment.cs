using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Sales.Domain.Enums;

namespace Tuilow.Sales.Domain.Entities;

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
        if (Status == PaymentStatus.Confirmed) return; // idempotente (webhook pode repetir evento)

        Status = PaymentStatus.Confirmed;
        PaidAt = DateTime.UtcNow;
        Touch();
    }

    public void Fail() { Status = PaymentStatus.Failed; Touch(); }
    public void Refund()
    {
        if (Status == PaymentStatus.Refunded) return;

        Status = PaymentStatus.Refunded;
        Touch();
    }
}
