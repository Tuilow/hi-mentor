using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Catalog.Domain.ValueObjects;
using HiMentor.Payout.Domain.Enums;
using HiMentor.Payout.Domain.Events;

namespace HiMentor.Payout.Domain.Entities;

/// <summary>
/// Solicitação de saque feita por um criador sobre o saldo disponível de sua CreatorWallet
/// (módulo Finance). O ciclo de pagamento do HiMentor é quinzenal (dia 01-15 e dia 16-fim do
/// mês) — ver HiMentor.Finance.Domain.Common.PayoutCycleCalculator — mas a solicitação em si
/// pode ser feita a qualquer momento sobre o que já estiver disponível.
/// </summary>
public sealed class PayoutRequest : AggregateRoot
{
    private readonly List<PayoutTransaction> _transactions = [];

    public Guid CreatorId { get; private set; }
    public Money RequestedAmount { get; private set; } = null!;
    public PayoutRequestStatus Status { get; private set; } = PayoutRequestStatus.Pending;
    public DateOnly CycleStart { get; private set; }
    public DateOnly CycleEnd { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public IReadOnlyCollection<PayoutTransaction> Transactions => _transactions.AsReadOnly();

    private PayoutRequest() { }

    public static PayoutRequest Create(Guid creatorId, decimal amount, DateOnly cycleStart, DateOnly cycleEnd)
    {
        if (amount <= 0)
            throw new ArgumentException("O valor do saque deve ser maior que zero.", nameof(amount));

        var request = new PayoutRequest
        {
            CreatorId = creatorId,
            RequestedAmount = Money.Of(amount),
            CycleStart = cycleStart,
            CycleEnd = cycleEnd,
            RequestedAt = DateTime.UtcNow
        };

        request.AddDomainEvent(new PayoutRequestedDomainEvent(request.Id, creatorId, amount));
        return request;
    }

    public void Approve(Guid adminUserId)
    {
        EnsureStatus(PayoutRequestStatus.Pending, "aprovar");
        Status = PayoutRequestStatus.Approved;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        Touch();
    }

    public void Reject(Guid adminUserId, string? reason)
    {
        EnsureStatus(PayoutRequestStatus.Pending, "rejeitar");
        Status = PayoutRequestStatus.Rejected;
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = adminUserId;
        RejectionReason = reason?.Trim();
        Touch();
    }

    /// <summary>
    /// Marca o saque como efetivamente pago. Retorna o PayoutTransaction criado para o caller
    /// persistir explicitamente como Added (mesmo padrão usado em outras entidades filhas).
    /// </summary>
    public PayoutTransaction MarkPaid(string? externalReference)
    {
        if (Status != PayoutRequestStatus.Approved)
            throw new InvalidOperationException("Só é possível concluir um saque que já foi aprovado.");

        Status = PayoutRequestStatus.Paid;
        PaidAt = DateTime.UtcNow;
        Touch();

        var transaction = PayoutTransaction.Create(Id, RequestedAmount.Amount, externalReference);
        _transactions.Add(transaction);
        return transaction;
    }

    private void EnsureStatus(PayoutRequestStatus expected, string action)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Não é possível {action} um saque com status '{Status}'.");
    }
}
