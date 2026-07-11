using Tuilow.Learning.Application.Commands.CompleteLesson;
using Tuilow.Learning.Application.Commands.EnrollStudent;
using Tuilow.Learning.Application.Queries.GetContinueWatching;
using Tuilow.Learning.Application.Queries.GetEnrollmentProgress;
using Tuilow.Learning.Application.Queries.GetLessonHistory;
using Tuilow.Learning.Application.Queries.GetMyEnrollments;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Learning.Api.Controllers;

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

    /// <summary>Lista os cursos em que o aluno autenticado está matriculado (filtro "Matriculados").</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyEnrollments(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyEnrollmentsQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>"Continuar de onde parei" — a última aula assistida entre todos os cursos matriculados.</summary>
    [HttpGet("continue-watching")]
    public async Task<IActionResult> GetContinueWatching(CancellationToken ct)
    {
        var result = await sender.Send(new GetContinueWatchingQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Histórico de aulas assistidas — todas as aulas com progresso, entre todos os cursos.</summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetLessonHistory(CancellationToken ct)
    {
        var result = await sender.Send(new GetLessonHistoryQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }
}

public sealed record EnrollRequest(Guid CourseId);
public sealed record TrackProgressRequest(Guid LessonId, int WatchedSeconds, int TotalSeconds);
