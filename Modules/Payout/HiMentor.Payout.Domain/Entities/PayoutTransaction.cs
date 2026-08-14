using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Catalog.Domain.ValueObjects;

namespace HiMentor.Payout.Domain.Entities;

/// <summary>Registro do pagamento efetivo (transferência) de uma solicitação de saque aprovada.</summary>
public sealed class PayoutTransaction : Entity
{
    public Guid PayoutRequestId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public string? ExternalReference { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    private PayoutTransaction() { }

    public static PayoutTransaction Create(Guid payoutRequestId, decimal amount, string? externalReference) =>
        new()
        {
            PayoutRequestId = payoutRequestId,
            Amount = Money.Of(amount),
            ExternalReference = externalReference,
            ProcessedAt = DateTime.UtcNow
        };
}
