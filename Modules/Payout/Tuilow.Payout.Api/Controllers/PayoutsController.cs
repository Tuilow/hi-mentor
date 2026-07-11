using Tuilow.Payout.Application.Commands.RequestPayout;
using Tuilow.Payout.Application.Queries.GetMyPayoutHistory;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Payout.Api.Controllers;

/// <summary>Saques do criador autenticado sobre o saldo disponível de sua carteira (módulo Finance).</summary>
[ApiController]
[Route("api/v1/payouts")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class PayoutsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Solicita saque do saldo disponível (integral, se valor não informado).</summary>
    [HttpPost]
    public async Task<IActionResult> RequestPayout([FromBody] RequestPayoutRequest? request, CancellationToken ct)
    {
        var id = await sender.Send(new RequestPayoutCommand(currentUser.UserId!.Value, request?.Amount), ct);
        return Ok(new { id, message = "Solicitação de saque registrada. Aguarde aprovação da administração." });
    }

    /// <summary>Histórico de solicitações de saque do criador autenticado.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyHistory(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyPayoutHistoryQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }
}

public sealed record RequestPayoutRequest(decimal? Amount);
