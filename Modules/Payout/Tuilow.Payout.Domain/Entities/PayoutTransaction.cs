using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;

namespace Tuilow.Payout.Domain.Entities;

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
