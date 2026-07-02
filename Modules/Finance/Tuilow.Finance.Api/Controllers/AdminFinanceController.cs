using Tuilow.Finance.Application.Commands.UpdatePlatformFee;
using Tuilow.Finance.Application.Queries.GetCreatorFinancialDashboard;
using Tuilow.Finance.Application.Queries.GetCreatorSalesHistory;
using Tuilow.Finance.Application.Queries.GetPlatformRevenue;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Finance.Api.Controllers;

/// <summary>
/// Área administrativa do módulo Finance: definir o percentual de comissão da plataforma e
/// consultar receitas (da plataforma como um todo ou de um criador específico).
/// </summary>
[ApiController]
[Route("api/v1/admin/finance")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public sealed class AdminFinanceController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Define um novo percentual de comissão da plataforma (fica valendo a partir de agora).</summary>
    [HttpPut("fee")]
    public async Task<IActionResult> UpdateFee([FromBody] UpdateFeeRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new UpdatePlatformFeeCommand(request.Percentage, currentUser.UserId!.Value, request.Notes), ct);
        return Ok(new { id });
    }

    /// <summary>Receita total retida pela plataforma (comissões) em um período — visão consolidada.</summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetPlatformRevenue([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await sender.Send(new GetPlatformRevenueQuery(from, to), ct);
        return Ok(result);
    }

    /// <summary>Receita (bruta/comissão/líquida) de um criador específico — auditoria administrativa.</summary>
    [HttpGet("creators/{creatorId:guid}/revenue")]
    public async Task<IActionResult> GetCreatorRevenue(Guid creatorId, CancellationToken ct)
    {
        var dashboard = await sender.Send(new GetCreatorFinancialDashboardQuery(creatorId), ct);
        return Ok(dashboard);
    }

    /// <summary>Extrato de vendas de um criador específico — auditoria administrativa.</summary>
    [HttpGet("creators/{creatorId:guid}/sales")]
    public async Task<IActionResult> GetCreatorSales(Guid creatorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
    {
        var result = await sender.Send(new GetCreatorSalesHistoryQuery(creatorId, from, to), ct);
        return Ok(result);
    }
}

public sealed record UpdateFeeRequest(decimal Percentage, string? Notes = null);
