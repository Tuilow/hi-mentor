using Tuilow.Sales.Domain.Enums;
using MediatR;

namespace Tuilow.Sales.Application.Commands.CreateCourseSubscriptionPlan;

/// <summary>
/// Passo 5 do assistente ("Preço") — opção "Assinatura". Cria (ou substitui) o plano de
/// assinatura recorrente do produto. Reaproveita a mesma entidade Plan/fluxo de cobrança do
/// modelo legado de assinatura da plataforma — só marca o plano com o CourseId do produto.
/// </summary>
public sealed record CreateCourseSubscriptionPlanCommand(
    Guid CourseId,
    Guid InstructorId,
    decimal Price,
    BillingCycle BillingCycle,
    int TrialDays = 0
) : IRequest<Guid>;
