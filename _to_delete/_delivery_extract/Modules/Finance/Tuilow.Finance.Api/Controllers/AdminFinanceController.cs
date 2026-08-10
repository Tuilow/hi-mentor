using Tuilow.Finance.Application.Commands.AdminSetCreatorAsaasAccountEnabled;
using Tuilow.Finance.Application.Commands.AdminSetCreatorCommissionOverride;
using Tuilow.Finance.Application.Commands.AdminSetCreatorOnboardingBlocked;
using Tuilow.Finance.Application.Commands.UpdatePlatformFee;
using Tuilow.Finance.Application.Queries.AdminListCreatorAsaasAccounts;
using Tuilow.Finance.Application.Queries.AdminListCreatorOnboardings;
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

    // ─── Marketplace de split — contas Asaas dos criadores ─────────────────────

    /// <summary>Lista as contas Asaas conectadas pelos criadores (marketplace de split) — WalletId/CPF mascarados, API Key nunca exposta.</summary>
    [HttpGet("asaas-accounts")]
    public async Task<IActionResult> ListAsaasAccounts([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await sender.Send(new AdminListCreatorAsaasAccountsQuery(skip, take), ct);
        return Ok(result);
    }

    /// <summary>Liga/desliga manualmente a capacidade de um criador vender via marketplace (ex.: suspeita de fraude).</summary>
    [HttpPut("asaas-accounts/{creatorAsaasAccountId:guid}/enabled")]
    public async Task<IActionResult> SetAsaasAccountEnabled(Guid creatorAsaasAccountId, [FromBody] SetEnabledRequest request, CancellationToken ct)
    {
        await sender.Send(new AdminSetCreatorAsaasAccountEnabledCommand(creatorAsaasAccountId, request.Enabled), ct);
        return NoContent();
    }

    /// <summary>Define (ou remove, se Percentage vier nulo) um percentual de comissão específico para este criador.</summary>
    [HttpPut("asaas-accounts/{creatorAsaasAccountId:guid}/commission-override")]
    public async Task<IActionResult> SetCommissionOverride(Guid creatorAsaasAccountId, [FromBody] SetCommissionOverrideRequest request, CancellationToken ct)
    {
        await sender.Send(new AdminSetCreatorCommissionOverrideCommand(creatorAsaasAccountId, request.Percentage), ct);
        return NoContent();
    }

    // ─── Onboarding financeiro via subconta (novo modelo, ver CreatorAsaasSubaccount) ─────────

    /// <summary>Lista o pipeline de onboarding financeiro dos criadores (subconta Asaas/BaaS) — CPF/CNPJ mascarado, API Key nunca exposta.</summary>
    [HttpGet("onboardings")]
    public async Task<IActionResult> ListOnboardings([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var result = await sender.Send(new AdminListCreatorOnboardingsQuery(skip, take), ct);
        return Ok(result);
    }

    /// <summary>Bloqueia/desbloqueia manualmente a venda de um criador no novo modelo (ex.: suspeita de fraude, pedido do próprio criador).</summary>
    [HttpPut("onboardings/{creatorAsaasSubaccountId:guid}/blocked")]
    public async Task<IActionResult> SetOnboardingBlocked(Guid creatorAsaasSubaccountId, [FromBody] SetOnboardingBlockedRequest request, CancellationToken ct)
    {
        await sender.Send(new AdminSetCreatorOnboardingBlockedCommand(creatorAsaasSubaccountId, request.Blocked, request.Reason), ct);
        return NoContent();
    }
}

public sealed record SetEnabledRequest(bool Enabled);
public sealed record SetCommissionOverrideRequest(decimal? Percentage);
public sealed record SetOnboardingBlockedRequest(bool Blocked, string? Reason);

public sealed record UpdateFeeRequest(decimal Percentage, string? Notes = null);
