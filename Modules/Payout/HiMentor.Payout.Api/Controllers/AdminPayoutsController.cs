using HiMentor.Payout.Application.Commands.ApprovePayout;
using HiMentor.Payout.Application.Commands.CompletePayout;
using HiMentor.Payout.Application.Commands.RejectPayout;
using HiMentor.Payout.Application.Queries.GetPendingPayoutRequests;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.Payout.Api.Controllers;

/// <summary>
/// Área administrativa de saques: aprovar/rejeitar solicitações e confirmar pagamentos
/// realizados (transferência bancária/PIX feita fora da plataforma, nesta primeira versão).
/// </summary>
[ApiController]
[Route("api/v1/admin/payouts")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public sealed class AdminPayoutsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista solicitações de saque aguardando aprovação.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var result = await sender.Send(new GetPendingPayoutRequestsQuery(), ct);
        return Ok(result);
    }

    /// <summary>Aprova uma solicitação de saque.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await sender.Send(new ApprovePayoutCommand(id, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Saque aprovado." });
    }

    /// <summary>Rejeita uma solicitação de saque — devolve o valor reservado ao saldo disponível do criador.</summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPayoutRequest? request, CancellationToken ct)
    {
        await sender.Send(new RejectPayoutCommand(id, currentUser.UserId!.Value, request?.Reason), ct);
        return Ok(new { message = "Saque rejeitado. Saldo devolvido ao criador." });
    }

    /// <summary>Confirma que um saque aprovado foi efetivamente pago (transferência já realizada).</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompletePayoutRequest? request, CancellationToken ct)
    {
        await sender.Send(new CompletePayoutCommand(id, request?.ExternalReference), ct);
        return Ok(new { message = "Saque marcado como pago." });
    }
}

public sealed record RejectPayoutRequest(string? Reason);
public sealed record CompletePayoutRequest(string? ExternalReference);
