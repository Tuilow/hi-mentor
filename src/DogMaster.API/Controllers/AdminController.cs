using DogMaster.Application.Contexts.Catalog.Queries.GetCourseByIdAdmin;
using DogMaster.Application.Contexts.Catalog.Queries.ListCoursesAdmin;
using DogMaster.Application.Contexts.Identity.Commands.PromoteUser;
using DogMaster.Domain.Contexts.Identity.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogMaster.API.Controllers;

/// <summary>
/// Operações exclusivas de administradores.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Altera o role de um usuário (Student / Instructor / Admin).
    /// </summary>
    /// <remarks>
    /// Exemplo de body:
    ///   { "newRole": "Admin" }
    ///   { "newRole": "Instructor" }
    ///   { "newRole": "Student" }
    /// </remarks>
    [HttpPatch("users/{userId:guid}/role")]
    public async Task<IActionResult> ChangeUserRole(
        Guid userId,
        [FromBody] ChangeRoleRequest request,
        CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.NewRole, ignoreCase: true, out var role))
            return BadRequest(new { message = $"Role inválido. Use: Student, Instructor ou Admin." });

        await sender.Send(new PromoteUserCommand(userId, role), ct);
        return Ok(new { message = $"Usuário {userId} promovido para {role}." });
    }

    /// <summary>Lista todos os cursos (Draft + Published + Archived) para o painel admin.</summary>
    [HttpGet("courses")]
    public async Task<IActionResult> ListAllCourses(CancellationToken ct)
    {
        var courses = await sender.Send(new ListCoursesAdminQuery(), ct);
        return Ok(courses);
    }

    /// <summary>Retorna detalhes completos de um curso (incluindo Draft) pelo ID.</summary>
    [HttpGet("courses/{courseId:guid}")]
    public async Task<IActionResult> GetCourseDetail(Guid courseId, CancellationToken ct)
    {
        var course = await sender.Send(new GetCourseByIdAdminQuery(courseId), ct);
        return Ok(course);
    }
}

public sealed record ChangeRoleRequest(string NewRole);
