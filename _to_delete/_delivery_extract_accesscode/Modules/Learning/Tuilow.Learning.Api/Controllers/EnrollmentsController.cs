using Tuilow.Learning.Application.Commands.CompleteLesson;
using Tuilow.Learning.Application.Commands.EnrollFreeCourseAnonymous;
using Tuilow.Learning.Application.Commands.EnrollStudent;
using Tuilow.Learning.Application.Commands.RedeemAccessCode;
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

    /// <summary>
    /// Matrícula em curso grátis sem exigir cadastro completo prévio (achado B2 da avaliação de
    /// UX) — mesmo nível de fricção do checkout anônimo de curso pago (só nome/e-mail, sem
    /// senha): a conta é localizada ou criada automaticamente pelo e-mail informado, e o acesso
    /// chega por Magic Link. Quando quem chama já está logado, o front-end manda o token da
    /// sessão normalmente e o backend usa o UserId dela em vez do e-mail do formulário.
    /// </summary>
    [HttpPost("free")]
    [AllowAnonymous]
    public async Task<IActionResult> EnrollFree([FromBody] EnrollFreeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new EnrollFreeCourseAnonymousCommand(
            currentUser.UserId, request.CourseId, request.CustomerName, request.CustomerEmail), ct);
        return Ok(result);
    }

    /// <summary>
    /// Ativa o acesso de um programa a partir de um código (bloco "Tenho um código de acesso" no
    /// dashboard do aluno sem nenhum programa). Sempre exige login — diferente de EnrollFree, não
    /// há caminho anônimo aqui (o código já pressupõe que a pessoa tem conta).
    /// </summary>
    [HttpPost("redeem-code")]
    public async Task<IActionResult> RedeemCode([FromBody] RedeemAccessCodeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new RedeemAccessCodeCommand(currentUser.UserId!.Value, request.Code), ct);
        return Ok(result);
    }

    /// <summary>Registra progresso em uma aula.</summary>
    [HttpPost("{enrollmentId:guid}/progress")]
    public async Task<IActionResult> TrackProgress(
        Guid enrollmentId, [FromBody] TrackProgressRequest request, CancellationToken ct)
    {
        await sender.Send(new CompleteLessonCommand(
            currentUser.UserId!.Value, enrollmentId,
            request.LessonId, request.WatchedSeconds, request.TotalSeconds, request.ClientCapturedAt), ct);
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

/// <summary>CustomerName/CustomerEmail são ignorados pelo handler quando o chamador já está logado (achado B2).</summary>
public sealed record EnrollFreeRequest(Guid CourseId, string CustomerName, string CustomerEmail);

public sealed record RedeemAccessCodeRequest(string Code);

/// <summary>ClientCapturedAt (achado M6) — ver doc de CompleteLessonCommand.</summary>
public sealed record TrackProgressRequest(
    Guid LessonId, int WatchedSeconds, int TotalSeconds, DateTime? ClientCapturedAt = null);
