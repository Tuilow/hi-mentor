using Tuilow.Application.Contexts.Streaming.Commands.GetVideoUploadUrl;
using Tuilow.Application.Contexts.Streaming.Commands.LinkVideoToLesson;
using Tuilow.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.API.Controllers;

[ApiController]
[Route("api/v1/videos")]
[Produces("application/json")]
public sealed class VideosController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Gera um slot de upload direto no Cloudflare Stream.
    /// Retorna o videoId (nosso banco), cloudflareVideoId e uploadUrl (TUS — o browser envia o arquivo aqui).
    /// Fluxo: chame este endpoint → use uploadUrl para enviar o arquivo → chame /link-lesson.
    /// </summary>
    [HttpPost("upload-url")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> GetUploadUrl(CancellationToken ct)
    {
        var result = await sender.Send(new GetVideoUploadUrlCommand(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Vincula um vídeo já enviado ao Cloudflare a uma aula específica.
    /// IsPreview = true  → aula gratuita (preview sem assinatura)
    /// IsPreview = false → exige assinatura ativa para assistir
    /// </summary>
    [HttpPost("{videoId:guid}/link-lesson")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> LinkToLesson(
        Guid videoId,
        [FromBody] LinkLessonRequest request,
        CancellationToken ct)
    {
        await sender.Send(new LinkVideoToLessonCommand(
            request.CourseId, request.ModuleId, request.LessonId,
            videoId, request.IsPreview), ct);

        return Ok(new { message = "Vídeo vinculado à aula com sucesso." });
    }
}

public sealed record LinkLessonRequest(
    Guid CourseId,
    Guid ModuleId,
    Guid LessonId,
    bool IsPreview = false
);
