using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Events;

namespace Tuilow.Sales.Domain.Entities;

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

    // Cancelamento não revoga o acesso na hora: o aluno mantém acesso até o fim do período já
    // pago (CurrentPeriodEnd), mesmo com Status já em Cancelled — consistente com a mensagem
    // exibida no cancelamento ("você terá acesso até o fim do período pago"). Depois de
    // CurrentPeriodEnd deixa de contar como ativo naturalmente, sem precisar de job agendado
    // para efetivar o cancelamento numa data futura.
    public bool IsActive => Status switch
    {
        SubscriptionStatus.Active or SubscriptionStatus.Trial => true,
        SubscriptionStatus.Cancelled => CurrentPeriodEnd > DateTime.UtcNow,
        _ => false
    };
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

    /// <summary>
    /// Confirma pagamento. Retorna o SubscriptionPayment caso um NOVO registro tenha sido criado
    /// (para o caller persistir explicitamente como Added — evita DbUpdateConcurrencyException).
    /// Retorna null se apenas atualizou um pagamento já existente.
    /// </summary>
    public SubscriptionPayment? ConfirmPayment(string asaasPaymentId, decimal amount)
    {
        var payment = _payments.SingleOrDefault(p => p.AsaasPaymentId == asaasPaymentId);

        // Idempotente: o mesmo pagamento já confirmado antes (reenvio de webhook, comportamento
        // normal de retry da Asaas) não deve reiniciar o período pago nem disparar de novo o
        // evento de confirmação (que reenviaria e-mail/magic link a cada retentativa) — mesma
        // guarda que já existe em CoursePurchase.ConfirmPayment.
        if (payment is not null && payment.Status == PaymentStatus.Confirmed)
            return null;

        SubscriptionPayment? newPayment = null;
        if (payment is null)
        {
            payment = SubscriptionPayment.Create(Id, asaasPaymentId, amount,
                DateOnly.FromDateTime(DateTime.UtcNow));
            _payments.Add(payment);
            newPayment = payment;
        }
        payment.Confirm();

        Status = SubscriptionStatus.Active;
        CurrentPeriodStart = DateTime.UtcNow;
        CurrentPeriodEnd = CalculatePeriodEnd(DateTime.UtcNow, BillingCycle);
        Touch();

        AddDomainEvent(new PaymentConfirmedDomainEvent(Id, UserId, asaasPaymentId, amount));
        return newPayment;
    }

    /// <summary>
    /// Marca pagamento como falho. Retorna o SubscriptionPayment caso um NOVO registro tenha
    /// sido criado (mesmo motivo de <see cref="ConfirmPayment"/>).
    /// </summary>
    public SubscriptionPayment? MarkPaymentFailed(string asaasPaymentId, decimal amount)
    {
        var payment = _payments.SingleOrDefault(p => p.AsaasPaymentId == asaasPaymentId);
        SubscriptionPayment? newPayment = null;
        if (payment is null)
        {
            payment = SubscriptionPayment.Create(Id, asaasPaymentId, amount, DateOnly.FromDateTime(DateTime.UtcNow));
            _payments.Add(payment);
            newPayment = payment;
        }

        payment.Fail();

        Status = SubscriptionStatus.PastDue;
        Touch();
        return newPayment;
    }

    public void Cancel(string? reason = null)
    {
        if (Status == SubscriptionStatus.Cancelled) return;

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
        BillingCycle.Semiannual => from.AddMonths(6),
        BillingCycle.Annual => from.AddYears(1),
        _ => from.AddMonths(1)
    };
}
