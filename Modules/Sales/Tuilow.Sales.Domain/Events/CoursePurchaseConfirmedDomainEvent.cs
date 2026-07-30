using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Sales.Domain.Events;

/// <summary>
/// Disparado quando o pagamento de uma compra avulsa de curso é confirmado pelo Asaas.
/// Consumido pelo módulo Finance (fora do bounded context de Sales) para calcular a comissão
/// da plataforma e creditar a carteira do criador — ver Tuilow.Finance.Application.EventHandlers.
/// </summary>
/// <param name="AsaasPaymentId">
/// Adicionado para o achado M12 da auditoria: sem isso, não havia como correlacionar
/// matrícula (Enrollment) + notificação (NotificationLog) + pagamento pelo mesmo identificador
/// externo — support tinha que cruzar CoursePurchaseId manualmente entre módulos.
/// </param>
public sealed record CoursePurchaseConfirmedDomainEvent(
    Guid CoursePurchaseId, Guid StudentId, Guid CourseId, Guid CreatorId, decimal Amount, string AsaasPaymentId
) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
