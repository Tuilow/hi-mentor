using Tuilow.Application.Contexts.Catalog.Queries.GetCourseByIdAdmin;
using Tuilow.Application.Contexts.Catalog.Queries.ListCoursesAdmin;
using Tuilow.Application.Contexts.Identity.Commands.PromoteUser;
using Tuilow.Application.Contexts.Identity.Commands.RemoveRole;
using Tuilow.Domain.Contexts.Identity.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.API.Controllers;

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
    /// Atribui um role a um usuário. Multi-role: não remove os roles existentes
    /// (ex.: um usuário pode acumular Student + Creator).
    /// </summary>
    /// <remarks>
    /// Exemplo de body:
    ///   { "roleName": "Admin" }
    ///   { "roleName": "Creator" }
    ///   { "roleName": "Student" }
    ///   { "roleName": "ChannelMember" }
    /// </remarks>
    [HttpPatch("users/{userId:guid}/role")]
    public async Task<IActionResult> AssignUserRole(
        Guid userId,
        [FromBody] ChangeRoleRequest request,
        CancellationToken ct)
    {
        if (!RoleNames.IsValid(request.RoleName))
            return BadRequest(new { message = $"Role inválido. Use um de: {string.Join(", ", RoleNames.All)}." });

        await sender.Send(new PromoteUserCommand(userId, request.RoleName), ct);
        return Ok(new { message = $"Role {request.RoleName} atribuído ao usuário {userId}." });
    }

    /// <summary>Revoga um role de um usuário.</summary>
    [HttpDelete("users/{userId:guid}/role/{roleName}")]
    public async Task<IActionResult> RemoveUserRole(Guid userId, string roleName, CancellationToken ct)
    {
        await sender.Send(new RemoveRoleCommand(userId, roleName), ct);
        return Ok(new { message = $"Role {roleName} removido do usuário {userId}." });
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

public sealed record ChangeRoleRequest(string RoleName);
