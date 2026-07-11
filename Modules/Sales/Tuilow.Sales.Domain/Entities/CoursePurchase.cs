using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Events;

namespace Tuilow.Sales.Domain.Entities;

/// <summary>
/// Compra avulsa de um curso por um aluno — pagamento único (não recorrente), diferente de
/// <see cref="Subscription"/> (assinatura da plataforma, modelo legado). É sobre esta entidade
/// que a comissão da plataforma é calculada: cada CoursePurchase confirmada gera um crédito
/// líquido para a carteira do criador (ver módulo Finance).
/// </summary>
public sealed class CoursePurchase : AggregateRoot
{
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CreatorId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public CoursePurchaseStatus Status { get; private set; } = CoursePurchaseStatus.Pending;
    public string AsaasCustomerId { get; private set; } = string.Empty;
    public string AsaasPaymentId { get; private set; } = string.Empty;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    private CoursePurchase() { }

    public static CoursePurchase Create(
        Guid studentId, Guid courseId, Guid creatorId, decimal amount,
        string asaasCustomerId, string asaasPaymentId)
    {
        return new CoursePurchase
        {
            StudentId = studentId,
            CourseId = courseId,
            CreatorId = creatorId,
            Amount = Money.Of(amount),
            AsaasCustomerId = asaasCustomerId,
            AsaasPaymentId = asaasPaymentId,
            Status = CoursePurchaseStatus.Pending
        };
    }

    public void ConfirmPayment()
    {
        if (Status == CoursePurchaseStatus.Confirmed) return; // idempotente (webhook pode repetir evento)

        Status = CoursePurchaseStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePurchaseConfirmedDomainEvent(Id, StudentId, CourseId, CreatorId, Amount.Amount));
    }

    public void MarkFailed()
    {
        // Guarda de estado: um webhook atrasado/fora de ordem (ex.: PAYMENT_OVERDUE chegando
        // depois de um PAYMENT_CONFIRMED já processado) não pode revogar o acesso de um aluno
        // que já pagou, nem sobrescrever um reembolso já registrado.
        if (Status != CoursePurchaseStatus.Pending) return;

        Status = CoursePurchaseStatus.Failed;
        Touch();
    }

    public void Refund()
    {
        if (Status != CoursePurchaseStatus.Confirmed)
            throw new InvalidOperationException("Só é possível reembolsar uma compra confirmada.");

        Status = CoursePurchaseStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePurchaseRefundedDomainEvent(Id, CreatorId, Amount.Amount));
    }
}
