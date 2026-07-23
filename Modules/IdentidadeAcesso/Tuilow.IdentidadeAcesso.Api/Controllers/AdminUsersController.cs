using Tuilow.IdentidadeAcesso.Application.Commands.DeleteUser;
using Tuilow.IdentidadeAcesso.Application.Commands.ReactivateUser;
using Tuilow.IdentidadeAcesso.Application.Commands.SuspendUser;
using Tuilow.IdentidadeAcesso.Application.Queries.GetPlatformStats;
using Tuilow.IdentidadeAcesso.Application.Queries.ListUsers;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.IdentidadeAcesso.Api.Controllers;

/// <summary>
/// Painel do dono da plataforma: listagem/gestão de usuários (ativar, suspender, excluir) e
/// visão geral de estatísticas. Compartilha o prefixo de rota com RolesController (gestão de
/// roles) — templates diferentes, sem colisão.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminUsersController(ISender sender) : ControllerBase
{
    /// <summary>Listagem paginada de usuários — busca por nome/e-mail, filtro por role e status.</summary>
    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        UserStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<UserStatus>(status, ignoreCase: true, out var parsedStatus))
                return BadRequest(new { message = $"Status inválido. Use um de: {string.Join(", ", Enum.GetNames<UserStatus>())}." });
            statusFilter = parsedStatus;
        }

        var result = await sender.Send(new ListUsersQuery(search, role, statusFilter, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Visão geral: contagens de usuários, criadores, atividade recente, cursos e vídeos.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await sender.Send(new GetPlatformStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>Suspende uma conta — bloqueia login e revoga sessões ativas.</summary>
    [HttpPut("{userId:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid userId, CancellationToken ct)
    {
        await sender.Send(new SuspendUserCommand(userId), ct);
        return Ok(new { message = $"Usuário {userId} suspenso." });
    }

    /// <summary>Reverte uma suspensão (ou reativa uma conta previamente excluída).</summary>
    [HttpPut("{userId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(Guid userId, CancellationToken ct)
    {
        await sender.Send(new ReactivateUserCommand(userId), ct);
        return Ok(new { message = $"Usuário {userId} reativado." });
    }

    /// <summary>
    /// Exclui a conta (soft-delete) e apaga permanentemente todos os vídeos do criador,
    /// arquivando os cursos dele.
    /// </summary>
    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken ct)
    {
        await sender.Send(new DeleteUserCommand(userId), ct);
        return Ok(new { message = $"Usuário {userId} excluído." });
    }
}
