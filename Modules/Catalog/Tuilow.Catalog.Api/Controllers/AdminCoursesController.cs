using Tuilow.Catalog.Application.Queries.GetCourseByIdAdmin;
using Tuilow.Catalog.Application.Queries.ListCoursesAdmin;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Catalog.Api.Controllers;

/// <summary>
/// Endpoints administrativos do catálogo (sem filtro de status — inclui Draft/Archived).
/// Split de Tuilow.API.Controllers.AdminController (as rotas de curso que viviam lá).
/// Painel "Gerenciar Cursos" do próprio Creator — sempre filtrado pelo usuário autenticado
/// (ver ListCoursesAdminQuery/GetCourseByIdAdminQuery), nunca lista/abre curso de outro criador.
/// </summary>
[ApiController]
[Route("api/v1/admin/courses")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class AdminCoursesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista os cursos (qualquer status) do Creator autenticado para o painel admin.</summary>
    [HttpGet]
    public async Task<IActionResult> ListAll(CancellationToken ct)
    {
        var result = await sender.Send(new ListCoursesAdminQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Detalhe de um curso por ID, sem filtro de status — só do próprio criador.</summary>
    [HttpGet("{courseId:guid}")]
    public async Task<IActionResult> GetById(Guid courseId, CancellationToken ct)
    {
        var course = await sender.Send(new GetCourseByIdAdminQuery(courseId, currentUser.UserId!.Value), ct);
        return Ok(course);
    }
}
