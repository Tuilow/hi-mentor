using Tuilow.Application.Contexts.Subscription.Commands.CancelSubscription;
using Tuilow.Application.Contexts.Subscription.Commands.CreateSubscription;
using Tuilow.Application.Contexts.Subscription.Queries.GetUserSubscription;
using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Contexts.Subscription.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.API.Controllers;

[ApiController]
[Route("api/v1/subscriptions")]
[Produces("application/json")]
public sealed class SubscriptionsController(
    ISender sender,
    ICurrentUserService currentUser,
    ISubscriptionRepository subscriptionRepo
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
}

public sealed record SubscribeRequest(Guid PlanId, string CustomerName, string CustomerEmail,
    string? CpfCnpj = null, string? Phone = null);
public sealed record CancelRequest(string? Reason);
