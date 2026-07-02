using Tuilow.Catalog.Application.Queries.GetCourseByIdAdmin;
using Tuilow.Catalog.Application.Queries.ListCoursesAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Catalog.Api.Controllers;

/// <summary>
/// Endpoints administrativos do catálogo (sem filtro de status — inclui Draft/Archived).
/// Split de Tuilow.API.Controllers.AdminController (as rotas de curso que viviam lá).
/// </summary>
[ApiController]
[Route("api/v1/admin/courses")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class AdminCoursesController(ISender sender) : ControllerBase
{
    /// <summary>Lista todos os cursos (qualquer status) para o painel admin.</summary>
    [HttpGet]
    public async Task<IActionResult> ListAll(CancellationToken ct)
    {
        var result = await sender.Send(new ListCoursesAdminQuery(), ct);
        return Ok(result);
    }

    /// <summary>Detalhe de um curso por ID, sem filtro de status.</summary>
    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> GetById(Guid courseId, CancellationToken ct)
    {
        var course = await sender.Send(new GetCourseByIdAdminQuery(courseId), ct);
        return Ok(course);
    }
}
