using MediatR;

namespace Tuilow.Sales.Application.Commands.SubscribeToCourse;

/// <summary>
/// Assinatura de UM produto específico (plano criado pelo criador no passo "Preço" do
/// assistente — ver CreateCourseSubscriptionPlanCommandHandler), comprada direto da Página de
/// Vendas pública (/c/[slug]). Espelha o checkout anônimo de
/// <see cref="Tuilow.Sales.Application.Commands.PurchaseCourse.PurchaseCourseCommand"/> (compra
/// avulsa): UserId é opcional — quando null (visitante sem login), o handler localiza ou cria a
/// conta pelo e-mail informado, e o acesso pós-pagamento chega por Magic Link, sem senha.
///
/// Diferente de <see cref="Tuilow.Sales.Application.Commands.CreateSubscription.CreateSubscriptionCommand"/>
/// (assinatura da plataforma/plano legado, exige login e recebe PlanId direto): aqui o plano é
/// resolvido a partir do CourseId, porque a Página de Vendas pública só conhece o curso — nunca
/// expõe o Id interno do Plan.
/// </summary>
public sealed record SubscribeToCourseCommand(
    Guid? UserId,
    Guid CourseId,
    string CustomerName,
    string CustomerEmail,
    string? CpfCnpj = null,
    string? Phone = null
) : IRequest<SubscribeToCourseResponse>;

public sealed record SubscribeToCourseResponse(
    Guid SubscriptionId,
    string AsaasSubscriptionId,
    string? PaymentUrl   // invoiceUrl do Asaas — onde o cliente paga via PIX/cartão/boleto
);
