using Tuilow.Sales.Application.Commands.CancelSubscription;
using Tuilow.Sales.Application.Commands.CreateCourseSubscriptionPlan;
using Tuilow.Sales.Application.Commands.CreateSubscription;
using Tuilow.Sales.Application.Commands.SimulateCourseSubscriptionPayment;
using Tuilow.Sales.Application.Commands.SubscribeToCourse;
using Tuilow.Sales.Application.Queries.GetUserSubscription;
using Tuilow.Sales.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Tuilow.Sales.Api.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
public sealed class SubscriptionsController(
    ISender sender,
    ICurrentUserService currentUser,
    ISubscriptionRepository subscriptionRepo,
    IHostEnvironment env
) : ControllerBase
{
    /// <summary>Lista planos disponíveis.</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans(CancellationToken ct)
    {
        var plans = await subscriptionRepo.GetActivePlansAsync(ct);
        return Ok(plans.Select(p => new
        {
            p.Id, p.Name, p.Slug, p.Description,
            Price = p.Price.Amount,
            BillingCycle = p.BillingCycle.ToString(),
            p.TrialDays,
            Features = p.Features.Select(f => new { f.FeatureKey, f.FeatureValue, f.DisplayName })
        }));
    }

    /// <summary>Retorna assinatura ativa do usuário autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscription(CancellationToken ct)
    {
        var sub = await sender.Send(new GetUserSubscriptionQuery(currentUser.UserId!.Value), ct);
        if (sub is null) return NotFound(new { message = "Nenhuma assinatura ativa encontrada." });
        return Ok(sub);
    }

    /// <summary>Cria nova assinatura para o usuário autenticado.</summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateSubscriptionCommand(
            currentUser.UserId!.Value, request.PlanId,
            request.CustomerName, request.CustomerEmail,
            request.CpfCnpj, request.Phone), ct);
        return Ok(result);
    }

    /// <summary>Cancela assinatura do usuário autenticado.</summary>
    [HttpDelete("me")]
    [Authorize]
    public async Task<IActionResult> Cancel([FromBody] CancelRequest? request, CancellationToken ct)
    {
        await sender.Send(new CancelSubscriptionCommand(currentUser.UserId!.Value, request?.Reason), ct);
        return Ok(new { message = "Assinatura cancelada. Você terá acesso até o fim do período pago." });
    }

    /// <summary>Plano de assinatura do produto (se o criador tiver escolhido "Assinatura" no passo de preço).</summary>
    [HttpGet("plans/by-course/{courseId:guid}")]
    public async Task<IActionResult> GetPlansByCourse(Guid courseId, CancellationToken ct)
    {
        var plans = await subscriptionRepo.GetPlansByCourseAsync(courseId, ct);
        return Ok(plans.Where(p => p.IsActive).Select(p => new
        {
            p.Id, p.Name, p.Description,
            Price = p.Price.Amount,
            BillingCycle = p.BillingCycle.ToString(),
            p.TrialDays
        }));
    }

    /// <summary>Define/atualiza o plano de assinatura do produto (passo 5 do assistente — opção "Assinatura").</summary>
    [HttpPost("plans/by-course/{courseId:guid}")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> CreateCourseSubscriptionPlan(Guid courseId,
        [FromBody] CreateCourseSubscriptionPlanCommand command, CancellationToken ct)
    {
        var planId = await sender.Send(
            command with { CourseId = courseId, InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(new { id = planId });
    }

    /// <summary>
    /// Assina o plano de um produto específico direto da Página de Vendas pública. Não exige
    /// login: o visitante assina com nome/e-mail, e a conta é localizada ou criada
    /// automaticamente (checkout anônimo — ver SubscribeToCourseCommandHandler). Espelha
    /// CoursePurchasesController.Purchase (compra avulsa), só que para o modelo "Assinatura".
    /// </summary>
    [HttpPost("course/{courseId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubscribeToCourse(Guid courseId,
        [FromBody] SubscribeToCourseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SubscribeToCourseCommand(
            currentUser.UserId, courseId,
            request.CustomerName, request.CustomerEmail,
            request.CpfCnpj, request.Phone), ct);
        return Ok(result);
    }

    /// <summary>
    /// SANDBOX/DEV apenas: simula a confirmação do pagamento da primeira cobrança da assinatura
    /// no lugar do webhook do Asaas, que não alcança localhost. Indisponível fora de Development
    /// (404) — nunca existe em produção. Não exige login, pelo mesmo motivo de
    /// CoursePurchasesController.SimulatePayment (checkout anônimo).
    /// </summary>
    [HttpPost("{id:guid}/simulate-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulateSubscriptionPayment(Guid id, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        await sender.Send(new SimulateCourseSubscriptionPaymentCommand(currentUser.UserId, id), ct);
        return Ok();
    }
}

public sealed record SubscribeRequest(Guid PlanId, string CustomerName, string CustomerEmail,
    string? CpfCnpj = null, string? Phone = null);
public sealed record SubscribeToCourseRequest(string CustomerName, string CustomerEmail,
    string? CpfCnpj = null, string? Phone = null);
public sealed record CancelRequest(string? Reason);
