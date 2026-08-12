using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Sales.Domain.Enums;

namespace Tuilow.Sales.Domain.Events;

/// <summary>
/// Disparado quando uma compra de curso previamente confirmada é reembolsada.
/// Consumido pelo módulo Finance para estornar o valor líquido creditado ao criador — só no
/// modelo Legacy. Em MarketplaceSplit a própria Asaas reverte o split automaticamente quando a
/// cobrança original é estornada, então não há nada para a Finance debitar.
///
/// Também consumido pelo módulo Learning (CoursePurchaseRefundedEventHandler) para revogar o
/// acesso do aluno, cancelando a matrícula (Enrollment) criada por
/// CoursePurchaseConfirmedEventHandler — StudentId/CourseId adicionados aqui (mesmo padrão de
/// CoursePurchaseConfirmedDomainEvent) porque o handler de Learning precisa localizar essa
/// matrícula. Antes desta correção, a Finance estornava o valor mas ninguém revogava o acesso
/// (achado encontrado investigando reclamação real de um estorno via Asaas, 12/08/2026).
/// </summary>
public sealed record CoursePurchaseRefundedDomainEvent(
    Guid CoursePurchaseId, Guid StudentId, Guid CourseId, Guid CreatorId, decimal Amount,
    CoursePurchasePaymentModel PaymentModel = CoursePurchasePaymentModel.Legacy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
