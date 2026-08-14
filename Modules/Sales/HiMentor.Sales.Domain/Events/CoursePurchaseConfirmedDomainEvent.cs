using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Sales.Domain.Enums;

namespace HiMentor.Sales.Domain.Events;

/// <summary>
/// Disparado quando o pagamento de uma compra avulsa de curso é confirmado pelo Asaas.
/// Consumido pelo módulo Finance (fora do bounded context de Sales) para calcular a comissão
/// da plataforma e creditar a carteira do criador (só no modelo Legacy — ver PaymentModel) e
/// pelo módulo Learning para liberar o acesso ao curso.
/// </summary>
/// <param name="AsaasPaymentId">
/// Correlaciona matrícula (Enrollment) + notificação (NotificationLog) + pagamento pelo mesmo
/// identificador externo.
/// </param>
/// <param name="PaymentModel">
/// Legacy ou MarketplaceSplit (ver CoursePurchase) — o handler de Finance usa isto para NAO
/// creditar a carteira interna do criador quando a venda já foi liquidada diretamente pelo split
/// da Asaas (o dinheiro nunca passou pela conta da HiMentor).
/// </param>
public sealed record CoursePurchaseConfirmedDomainEvent(
    Guid CoursePurchaseId, Guid StudentId, Guid CourseId, Guid CreatorId, decimal Amount, string AsaasPaymentId,
    CoursePurchasePaymentModel PaymentModel = CoursePurchasePaymentModel.Legacy
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
