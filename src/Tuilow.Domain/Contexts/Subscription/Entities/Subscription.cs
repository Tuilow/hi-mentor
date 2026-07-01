using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Contexts.Subscription.Enums;
using Tuilow.Domain.Contexts.Subscription.Events;

namespace Tuilow.Domain.Contexts.Subscription.Entities;

public sealed class Subscription : AggregateRoot
{
    private readonly List<SubscriptionPayment> _payments = [];

    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public string? AsaasSubscriptionId { get; private set; }
    public string? AsaasCustomerId { get; private set; }
    public BillingCycle BillingCycle { get; private set; }

    public bool IsActive => Status is SubscriptionStatus.Active or SubscriptionStatus.Trial;
    public IReadOnlyCollection<SubscriptionPayment> Payments => _payments.AsReadOnly();

    private Subscription() { }

    public static Subscription Create(Guid userId, Guid planId, BillingCycle billingCycle,
        string asaasCustomerId, string asaasSubscriptionId, int trialDays = 0)
    {
        var now = DateTime.UtcNow;
        var sub = new Subscription
        {
            UserId = userId,
            PlanId = planId,
            BillingCycle = billingCycle,
            AsaasCustomerId = asaasCustomerId,
            AsaasSubscriptionId = asaasSubscriptionId,
            Status = trialDays > 0 ? SubscriptionStatus.Trial : SubscriptionStatus.Active,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = trialDays > 0 ? now.AddDays(trialDays) : CalculatePeriodEnd(now, billingCycle)
        };

        sub.AddDomainEvent(new SubscriptionCreatedDomainEvent(sub.Id, userId, planId, billingCycle));
        return sub;
    }

    public void ConfirmPayment(string asaasPaymentId, decimal amount)
    {
        var payment = _payments.SingleOrDefault(p => p.AsaasPaymentId == asaasPaymentId);
        if (payment is null)
        {
            payment = SubscriptionPayment.Create(Id, asaasPaymentId, amount,
                DateOnly.FromDateTime(DateTime.UtcNow));
            _payments.Add(payment);
        }
        payment.Confirm();

        Status = SubscriptionStatus.Active;
        CurrentPeriodStart = DateTime.UtcNow;
        CurrentPeriodEnd = CalculatePeriodEnd(DateTime.UtcNow, BillingCycle);
        Touch();

        AddDomainEvent(new PaymentConfirmedDomainEvent(Id, UserId, asaasPaymentId, amount));
    }

    public void MarkPaymentFailed(string asaasPaymentId, decimal amount)
    {
        var payment = _payments.SingleOrDefault(p => p.AsaasPaymentId == asaasPaymentId)
            ?? SubscriptionPayment.Create(Id, asaasPaymentId, amount, DateOnly.FromDateTime(DateTime.UtcNow));

        payment.Fail();
        if (!_payments.Contains(payment)) _payments.Add(payment);

        Status = SubscriptionStatus.PastDue;
        Touch();
    }

    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancelReason = reason;
        Touch();
        AddDomainEvent(new SubscriptionCancelledDomainEvent(Id, UserId, reason));
    }

    private static DateTime CalculatePeriodEnd(DateTime from, BillingCycle cycle) => cycle switch
    {
        BillingCycle.Monthly => from.AddMonths(1),
        BillingCycle.Quarterly => from.AddMonths(3),
        BillingCycle.Annual => from.AddYears(1),
        _ => from.AddMonths(1)
    };
}
