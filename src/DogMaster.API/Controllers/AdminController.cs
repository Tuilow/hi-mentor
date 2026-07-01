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
}

public sealed record ChangeRoleRequest(string NewRole);
