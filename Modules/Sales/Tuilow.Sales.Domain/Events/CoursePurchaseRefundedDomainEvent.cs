using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Sales.Domain.Enums;

namespace Tuilow.Sales.Domain.Events;

/// <summary>
/// Disparado quando uma compra de curso previamente confirmada é reembolsada.
/// Consumido pelo módulo Finance para estornar o valor líquido creditado ao criador — só no
/// modelo Legacy. Em MarketplaceSplit a própria Asaas reverte o split automaticamente quando a
/// cobrança original é estornada, então não há nada para a Finance debitar.
/// </summary>
public sealed record CoursePurchaseRefundedDomainEvent(
    Guid CoursePurchaseId, Guid CreatorId, decimal Amount,
    CoursePurchasePaymentModel PaymentModel = CoursePurchasePaymentModel.Legacy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
