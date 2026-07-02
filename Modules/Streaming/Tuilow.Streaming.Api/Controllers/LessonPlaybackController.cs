using Tuilow.Streaming.Application.Queries.GetLessonPlayUrl;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Streaming.Api.Controllers;

/// <summary>
/// Split de Tuilow.API.Controllers.CoursesController — só o endpoint de playback, que
/// depende do contexto Streaming. Mantém a mesma rota (api/v1/courses/.../play) para não
/// quebrar o frontend.
/// </summary>
[ApiController]
[Route("api/v1/courses")]
[Produces("application/json")]
public sealed class LessonPlaybackController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Retorna URL de playback para uma aula.
    /// Preview → URL pública Cloudflare (sem autenticação).
    /// Pago → exige assinatura ativa; retorna URL JWT assinada (expira em 4h).
    /// </summary>
    [HttpGet("{courseId:guid}/lessons/{lessonId:guid}/play")]
    public async Task<IActionResult> GetLessonPlayUrl(Guid courseId, Guid lessonId, CancellationToken ct)
    {
        var result = await sender.Send(
            new GetLessonPlayUrlQuery(courseId, lessonId, currentUser.UserId), ct);
        return Ok(result);
    }
}
