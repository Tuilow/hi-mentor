using Tuilow.IdentidadeAcesso.Application.Commands.PromoteUser;
using Tuilow.IdentidadeAcesso.Application.Commands.RemoveRole;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.IdentidadeAcesso.Api.Controllers;

/// <summary>
/// Gestão de roles de usuário. Movido do antigo AdminController — os endpoints de
/// curso que estavam junto foram para o módulo Catalog.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class RolesController(ISender sender) : ControllerBase
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
    [HttpPatch("{userId:guid}/role")]
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
    [HttpDelete("{userId:guid}/role/{roleName}")]
    public async Task<IActionResult> RemoveUserRole(Guid userId, string roleName, CancellationToken ct)
    {
        await sender.Send(new RemoveRoleCommand(userId, roleName), ct);
        return Ok(new { message = $"Role {roleName} removido do usuário {userId}." });
    }
}

public sealed record ChangeRoleRequest(string RoleName);
