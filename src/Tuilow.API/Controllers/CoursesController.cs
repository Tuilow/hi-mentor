using Tuilow.Application.Contexts.Catalog.Commands.AddLesson;
using Tuilow.Application.Contexts.Catalog.Commands.AddModule;
using Tuilow.Application.Contexts.Catalog.Commands.CreateCourse;
using Tuilow.Application.Contexts.Catalog.Commands.PublishCourse;
using Tuilow.Application.Contexts.Catalog.Queries.GetCourseBySlug;
using Tuilow.Application.Contexts.Catalog.Queries.ListCourses;
using Tuilow.Application.Contexts.Streaming.Queries.GetLessonPlayUrl;
using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.API.Controllers;

[ApiController]
[Route("api/v1/courses")]
[Produces("application/json")]
public sealed class CoursesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista cursos publicados com filtros e paginação.</summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] CourseLevel? level,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new ListCoursesQuery(level, search, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Retorna detalhes de um curso pelo slug.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var course = await sender.Send(new GetCourseBySlugQuery(slug, currentUser.UserId), ct);
        return Ok(course);
    }

    /// <summary>Cria um novo curso (apenas instrutores/admin).</summary>
    [HttpPost]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCourseCommand command, CancellationToken ct)
    {
        var courseId = await sender.Send(
            command with { InstructorId = currentUser.UserId!.Value }, ct);
        return CreatedAtAction(nameof(GetBySlug), new { slug = "created" }, new { id = courseId });
    }

    /// <summary>Adiciona módulo ao curso.</summary>
    [HttpPost("{courseId:guid}/modules")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> AddModule(Guid courseId, [FromBody] AddModuleCommand command, CancellationToken ct)
    {
        var moduleId = await sender.Send(command with { CourseId = courseId }, ct);
        return Ok(new { id = moduleId });
    }

    /// <summary>Adiciona aula ao módulo.</summary>
    [HttpPost("{courseId:guid}/modules/{moduleId:guid}/lessons")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> AddLesson(Guid courseId, Guid moduleId,
        [FromBody] AddLessonCommand command, CancellationToken ct)
    {
        var lessonId = await sender.Send(
            command with { CourseId = courseId, ModuleId = moduleId }, ct);
        return Ok(new { id = lessonId });
    }

    /// <summary>Publica o curso (torna-o visível para alunos).</summary>
    [HttpPost("{courseId:guid}/publish")]
    [Authorize(Roles = "Instructor,Admin")]
    public async Task<IActionResult> Publish(Guid courseId, CancellationToken ct)
    {
        await sender.Send(new PublishCourseCommand(courseId, currentUser.UserId!.Value), ct);
        return Ok(new { message = "Curso publicado com sucesso." });
    }

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
