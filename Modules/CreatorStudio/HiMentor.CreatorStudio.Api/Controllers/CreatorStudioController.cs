using HiMentor.CreatorStudio.Application.Commands.CaptureLead;
using HiMentor.CreatorStudio.Application.Commands.DeleteRecordingTemplate;
using HiMentor.CreatorStudio.Application.Commands.GenerateCourseOutline;
using HiMentor.CreatorStudio.Application.Commands.GenerateLessonScript;
using HiMentor.CreatorStudio.Application.Commands.GenerateMarketingCopy;
using HiMentor.CreatorStudio.Application.Commands.GenerateProductCopy;
using HiMentor.CreatorStudio.Application.Commands.GenerateSalesPageCopy;
using HiMentor.CreatorStudio.Application.Commands.MarkScriptAsRecorded;
using HiMentor.CreatorStudio.Application.Commands.PublishProduct;
using HiMentor.CreatorStudio.Application.Commands.SaveLessonScript;
using HiMentor.CreatorStudio.Application.Commands.SaveRecordingTemplate;
using HiMentor.CreatorStudio.Application.Commands.SetCreatorNiche;
using HiMentor.CreatorStudio.Application.Interfaces;
using HiMentor.CreatorStudio.Application.Queries.GetCreatorStyleProfile;
using HiMentor.CreatorStudio.Application.Queries.GetMyLessonScripts;
using HiMentor.CreatorStudio.Application.Queries.GetMyProducts;
using HiMentor.CreatorStudio.Application.Queries.GetMyRecordingTemplates;
using HiMentor.CreatorStudio.Application.Queries.GetProductDashboard;
using HiMentor.CreatorStudio.Application.Queries.GetPublicationChecklist;
using HiMentor.CreatorStudio.Application.Queries.GetVideoEditingCapabilities;
using HiMentor.CreatorStudio.Domain.Enums;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HiMentor.CreatorStudio.Api.Controllers;

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

    /// <summary>
    /// Captura um lead da página de vendas pública — endpoint anônimo (sem exigir papel de
    /// Creator). Achado M9 da avaliação: sem limite de taxa, um script conseguia inundar o
    /// banco de leads falsos. EnableRateLimiting("leads") aplica a política registrada em
    /// Program.cs (5 requisições/10min por IP) — captcha explicitamente NÃO implementado,
    /// fora do escopo deste achado.
    /// </summary>
    [HttpPost("leads")]
    [AllowAnonymous]
    [EnableRateLimiting("leads")]
    public async Task<IActionResult> CaptureLead([FromBody] CaptureLeadCommand command, CancellationToken ct)
    {
        var leadId = await sender.Send(command, ct);
        return Ok(new { id = leadId });
    }

    /// <summary>Central de Divulgação — gera texto pronto por canal (Instagram/Stories/WhatsApp/E-mail/Ads/Headline).</summary>
    [HttpPost("products/{courseId:guid}/generate-marketing-copy")]
    public async Task<IActionResult> GenerateMarketingCopy(
        Guid courseId, [FromBody] GenerateMarketingCopyRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new GenerateMarketingCopyCommand(courseId, currentUser.UserId!.Value, request.Channel), ct);
        return Ok(result);
    }

    // ─── Estúdio do Criador ───────────────────────────────────────────────

    /// <summary>Passo 1 — perfil de nicho do criador autenticado (null se ainda não preencheu).</summary>
    [HttpGet("studio/niche")]
    public async Task<IActionResult> GetMyNiche(CancellationToken ct)
    {
        var result = await sender.Send(new GetCreatorStyleProfileQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Passo 1 — salva/atualiza o perfil de nicho (nicho, público-alvo, objetivo, nível).</summary>
    [HttpPut("studio/niche")]
    public async Task<IActionResult> SetMyNiche([FromBody] SetNicheRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new SetCreatorNicheCommand(
            currentUser.UserId!.Value, request.Niche, request.TargetAudience, request.Objective, request.Level), ct);
        return Ok(new { id });
    }

    /// <summary>Passo 2 — gera a estrutura do curso (nome, descrição, módulos e aulas) a partir do nicho.</summary>
    [HttpPost("studio/course-outline")]
    public async Task<IActionResult> GenerateCourseOutline([FromBody] GenerateCourseOutlineCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Passo 3 — gera o roteiro de gravação de uma aula específica.</summary>
    [HttpPost("studio/lesson-script")]
    public async Task<IActionResult> GenerateLessonScript([FromBody] GenerateLessonScriptCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>Tela "Meus Roteiros" — todos os roteiros salvos pelo criador autenticado.</summary>
    [HttpGet("studio/lesson-scripts")]
    public async Task<IActionResult> GetMyLessonScripts(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyLessonScriptsQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Salva (persiste) um roteiro gerado/editado pelo criador.</summary>
    [HttpPost("studio/lesson-scripts")]
    public async Task<IActionResult> SaveLessonScript([FromBody] SaveLessonScriptRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new SaveLessonScriptCommand(
            currentUser.UserId!.Value, request.LessonTitle, request.Introduction,
            request.DevelopmentTopics, request.DemonstrationSuggestions, request.ClosingCta,
            request.CourseId, request.LessonId), ct);
        return Ok(new { id });
    }

    /// <summary>Marca um roteiro como gravado — conta para o progresso do Clone do Professor.</summary>
    [HttpPost("studio/lesson-scripts/{scriptId:guid}/mark-recorded")]
    public async Task<IActionResult> MarkScriptAsRecorded(Guid scriptId, CancellationToken ct)
    {
        await sender.Send(new MarkScriptAsRecordedCommand(scriptId, currentUser.UserId!.Value), ct);
        return Ok();
    }

    /// <summary>Templates de gravação do criador autenticado.</summary>
    [HttpGet("studio/recording-templates")]
    public async Task<IActionResult> GetMyRecordingTemplates(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyRecordingTemplatesQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Cria (TemplateId nulo) ou atualiza um template de gravação.</summary>
    [HttpPut("studio/recording-templates")]
    public async Task<IActionResult> SaveRecordingTemplate([FromBody] SaveRecordingTemplateRequest request, CancellationToken ct)
    {
        var id = await sender.Send(new SaveRecordingTemplateCommand(
            currentUser.UserId!.Value, request.Name, request.Sections, request.IsDefault, request.TemplateId), ct);
        return Ok(new { id });
    }

    /// <summary>Remove um template de gravação do criador autenticado.</summary>
    [HttpDelete("studio/recording-templates/{templateId:guid}")]
    public async Task<IActionResult> DeleteRecordingTemplate(Guid templateId, CancellationToken ct)
    {
        await sender.Send(new DeleteRecordingTemplateCommand(templateId, currentUser.UserId!.Value), ct);
        return Ok();
    }

    /// <summary>O front consulta antes de mostrar os botões de edição automática/clipes (ou o aviso de "em breve").</summary>
    [HttpGet("studio/video-editing-capabilities")]
    public async Task<IActionResult> GetVideoEditingCapabilities(CancellationToken ct)
    {
        var result = await sender.Send(new GetVideoEditingCapabilitiesQuery(), ct);
        return Ok(result);
    }
}

public sealed record GenerateMarketingCopyRequest(MarketingChannel Channel);

public sealed record SetNicheRequest(string Niche, string TargetAudience, string Objective, AudienceLevel Level);

public sealed record SaveLessonScriptRequest(
    string LessonTitle,
    string Introduction,
    IReadOnlyList<string> DevelopmentTopics,
    IReadOnlyList<string> DemonstrationSuggestions,
    string ClosingCta,
    Guid? CourseId,
    Guid? LessonId);

public sealed record SaveRecordingTemplateRequest(
    string Name,
    IReadOnlyList<string> Sections,
    bool IsDefault,
    Guid? TemplateId);
