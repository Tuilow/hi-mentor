using Tuilow.Application.Contexts.Learning.Commands.CompleteLesson;
using Tuilow.Application.Contexts.Learning.Commands.EnrollStudent;
using Tuilow.Application.Contexts.Learning.Queries.GetEnrollmentProgress;
using Tuilow.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
[Authorize]
[Produces("application/json")]
public sealed class EnrollmentsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Matricula o usuário em um curso.</summary>
    [HttpPost]
    public async Task<IActionResult> Enroll([FromBody] EnrollRequest request, CancellationToken ct)
    {
        var enrollmentId = await sender.Send(
            new EnrollStudentCommand(currentUser.UserId!.Value, request.CourseId), ct);
        return Ok(new { enrollmentId });
    }

    /// <summary>Registra progresso em uma aula.</summary>
    [HttpPost("{enrollmentId:guid}/progress")]
    public async Task<IActionResult> TrackProgress(
        Guid enrollmentId, [FromBody] TrackProgressRequest request, CancellationToken ct)
    {
        await sender.Send(new CompleteLessonCommand(
            currentUser.UserId!.Value, enrollmentId,
            request.LessonId, request.WatchedSeconds, request.TotalSeconds), ct);
        return Ok();
    }

    /// <summary>Retorna progresso do aluno em um curso.</summary>
    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetProgress(Guid courseId, CancellationToken ct)
    {
        var progress = await sender.Send(
            new GetEnrollmentProgressQuery(currentUser.UserId!.Value, courseId), ct);
        if (progress is null) return NotFound();
        return Ok(progress);
    }
}

public sealed record EnrollRequest(Guid CourseId);
public sealed record TrackProgressRequest(Guid LessonId, int WatchedSeconds, int TotalSeconds);
