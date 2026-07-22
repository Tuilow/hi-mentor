using Tuilow.Streaming.Application.Commands.DeleteVideo;
using Tuilow.Streaming.Application.Commands.GetVideoUploadUrl;
using Tuilow.Streaming.Application.Commands.ImportExternalVideo;
using Tuilow.Streaming.Application.Commands.LinkVideoToLesson;
using Tuilow.Streaming.Application.Queries.GetVideosByCourse;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Streaming.Api.Controllers;

[ApiController]
[Route("api/v1/videos")]
[Produces("application/json")]
public sealed class VideosController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Gera um slot de upload direto no Cloudflare Stream.
    /// Retorna o videoId (nosso banco), cloudflareVideoId e uploadUrl (TUS — o browser envia o arquivo aqui).
    /// Fluxo: chame este endpoint → use uploadUrl para enviar o arquivo → chame /link-lesson.
    /// </summary>
    [HttpPost("upload-url")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> GetUploadUrl([FromBody] GetUploadUrlRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GetVideoUploadUrlCommand(request.CourseId, currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>
    /// Importa um vídeo já hospedado em outra plataforma (YouTube, Vimeo, Cloudflare Stream,
    /// Google Drive, Dropbox ou OneDrive) a partir da URL — passo 2 do assistente de criação.
    /// Estratégia da plataforma: preferir import a upload local sempre que possível, para não
    /// pagar armazenamento/transcodificação de algo que já está hospedado em outro lugar.
    /// Retorna um VideoId no mesmo formato do upload — o passo seguinte (vincular à aula) usa
    /// o endpoint /link-lesson normalmente, sem distinguir a origem do vídeo.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Import([FromBody] ImportExternalVideoCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command with { InstructorId = currentUser.UserId!.Value }, ct);
        return Ok(result);
    }

    /// <summary>
    /// Vídeos já enviados/importados para este produto (vinculados a uma aula ou não) — usado
    /// para reidratar o passo "Conteúdo" do assistente ao reabri-lo.
    /// </summary>
    [HttpGet("by-course/{courseId:guid}")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> GetByCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetVideosByCourseQuery(courseId, currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>
    /// Vincula um vídeo já enviado ao Cloudflare a uma aula específica.
    /// IsPreview = true  → aula gratuita (preview sem assinatura)
    /// IsPreview = false → exige assinatura ativa para assistir
    /// </summary>
    [HttpPost("{videoId:guid}/link-lesson")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> LinkToLesson(
        Guid videoId,
        [FromBody] LinkLessonRequest request,
        CancellationToken ct)
    {
        await sender.Send(new LinkVideoToLessonCommand(
            request.CourseId, currentUser.UserId!.Value, request.ModuleId, request.LessonId,
            videoId, request.IsPreview), ct);

        return Ok(new { message = "Vídeo vinculado à aula com sucesso." });
    }

    /// <summary>
    /// Remove um vídeo enviado/importado que ainda não foi vinculado a nenhuma aula (ex.:
    /// descartar um vídeo com erro no download do YouTube, ou um vídeo importado por engano).
    /// Bloqueia (com erro claro) se o vídeo já estiver vinculado a uma aula.
    /// </summary>
    [HttpDelete("{videoId:guid}")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> Delete(Guid videoId, CancellationToken ct)
    {
        await sender.Send(new DeleteVideoCommand(videoId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Vídeo removido com sucesso." });
    }
}

public sealed record LinkLessonRequest(
    Guid CourseId,
    Guid ModuleId,
    Guid LessonId,
    bool IsPreview = false
);

public sealed record GetUploadUrlRequest(Guid CourseId);
