using MediatR;

namespace Tuilow.Sales.Application.Commands.PurchaseCourse;

/// <summary>
/// Compra avulsa de um curso (pagamento único) — modelo principal de monetização do Tuilow.
/// Diferente de <see cref="CreateSubscription.CreateSubscriptionCommand"/> (assinatura da
/// plataforma, modelo legado), aqui o aluno paga apenas pelo curso específico.
/// </summary>
public sealed record PurchaseCourseCommand(
    Guid StudentId,
    Guid CourseId,
    string CustomerName,
    string CustomerEmail,
    string? CpfCnpj = null,
    string? Phone = null
) : IRequest<PurchaseCourseResponse>;

public sealed record PurchaseCourseResponse(
    Guid CoursePurchaseId,
    string AsaasPaymentId,
    string? PaymentUrl
);
