using Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;
using Tuilow.Finance.Application.Queries.GetCreatorSalesHistory;
using Tuilow.Finance.Application.Queries.GetCurrentPlatformFee;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Finance.Api.Controllers;

/// <summary>Painel financeiro do criador autenticado — saldo, vendas do período e ciclo de pagamento.</summary>
[ApiController]
[Route("api/v1/finance")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class FinanceController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Resumo financeiro: saldo disponível, saldo pendente, totais e próximo pagamento.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await sender.Send(new GetCreatorFinancialDashboardQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Extrato de vendas/lançamentos do criador, opcionalmente filtrado por período.</summary>
    [HttpGet("sales")]
    public async Task<IActionResult> GetSalesHistory([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await sender.Send(new GetCreatorSalesHistoryQuery(currentUser.UserId!.Value, from, to), ct);
        return Ok(result);
    }

    /// <summary>Percentual de comissão da plataforma vigente — para exibir de forma transparente ao criador.</summary>
    [HttpGet("fee")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCurrentFee(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentPlatformFeeQuery(), ct);
        return Ok(result);
    }
}
