using Tuilow.CreatorStudio.Application.Commands.CaptureLead;
using Tuilow.CreatorStudio.Application.Commands.GenerateProductCopy;
using Tuilow.CreatorStudio.Application.Commands.GenerateSalesPageCopy;
using Tuilow.CreatorStudio.Application.Commands.PublishProduct;
using Tuilow.CreatorStudio.Application.Queries.GetMyProducts;
using Tuilow.CreatorStudio.Application.Queries.GetProductDashboard;
using Tuilow.CreatorStudio.Application.Queries.GetPublicationChecklist;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.CreatorStudio.Api.Controllers;

/// <summary>
/// Orquestra a Jornada Guiada de Criação de Produtos: "Meus Produtos" (hub do criador),
/// geração de copy por IA (sempre sugestão — quem aplica é o próprio front, usando os
/// commands já existentes de Catalog), publicação (com checklist) e dashboard do produto.
/// Não duplica dado nenhum de Catalog/Sales/Learning/Finance — só compõe.
/// </summary>
[ApiController]
[Route("api/v1/creator-studio")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class CreatorStudioController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Tela "Meus Produtos" — todos os produtos do criador autenticado.</summary>
    [HttpGet("my-products")]
    public async Task<IActionResult> GetMyProducts(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyProductsQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Passo 1 do assistente — "Gerar com IA" (copy do produto). Não exige produto já criado.</summary>
    [HttpPost("generate-product-copy")]
    public async Task<IActionResult> GenerateProductCopy([FromBody] GenerateProductCopyCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Passo 6 do assistente — sugestão de página de vendas.</summary>
    [HttpPost("products/{courseId:guid}/generate-sales-page-copy")]
    public async Task<IActionResult> GenerateSalesPageCopy(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(
            new GenerateSalesPageCopyCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Checklist do passo 7 (Publicação) — para o front mostrar os ✓/pendentes antes de publicar.</summary>
    [HttpGet("products/{courseId:guid}/publication-checklist")]
    public async Task<IActionResult> GetPublicationChecklist(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetPublicationChecklistQuery(courseId, currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Passo 7 do assistente — botão "Publicar Produto".</summary>
    [HttpPost("products/{courseId:guid}/publish")]
    public async Task<IActionResult> PublishProduct(Guid courseId, CancellationToken ct)
    {
        await sender.Send(new PublishProductCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Produto publicado com sucesso." });
    }

    /// <summary>Dashboard pós-publicação do produto.</summary>
    [HttpGet("products/{courseId:guid}/dashboard")]
    public async Task<IActionResult> GetProductDashboard(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetProductDashboardQuery(courseId, currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Captura um lead da página de vendas pública — endpoint anônimo (sem exigir papel de Creator).</summary>
    [HttpPost("leads")]
    [AllowAnonymous]
    public async Task<IActionResult> CaptureLead([FromBody] CaptureLeadCommand command, CancellationToken ct)
    {
        var leadId = await sender.Send(command, ct);
        return Ok(new { id = leadId });
    }
}
