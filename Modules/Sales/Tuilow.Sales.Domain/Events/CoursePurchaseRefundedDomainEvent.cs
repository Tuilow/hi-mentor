using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Sales.Domain.Events;

/// <summary>
/// Disparado quando uma compra de curso previamente confirmada é reembolsada.
/// Consumido pelo módulo Finance para estornar o valor líquido creditado ao criador.
/// </summary>
public sealed record CoursePurchaseRefundedDomainEvent(
    Guid CoursePurchaseId, Guid CreatorId, decimal Amount
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
