using MediatR;

namespace HiMentor.Sales.Application.Commands.PurchaseCourse;

/// <summary>
/// Compra avulsa de um curso (pagamento único) — modelo principal de monetização do HiMentor.
/// Diferente de <see cref="CreateSubscription.CreateSubscriptionCommand"/> (assinatura da
/// plataforma, modelo legado), aqui o aluno paga apenas pelo curso específico.
///
/// Checkout anônimo: StudentId é opcional — quando null (visitante sem login), o handler
/// localiza ou cria a conta pelo e-mail informado (ver IUserProvisioningService) e o aluno
/// recebe o acesso pós-pagamento via Magic Link, sem precisar de senha.
/// </summary>
public sealed record PurchaseCourseCommand(
    Guid? StudentId,
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
