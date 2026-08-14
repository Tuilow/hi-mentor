using HiMentor.IdentidadeAcesso.Application.Commands.DeleteUser;
using HiMentor.IdentidadeAcesso.Application.Commands.ReactivateUser;
using HiMentor.IdentidadeAcesso.Application.Commands.SuspendUser;
using HiMentor.IdentidadeAcesso.Application.Commands.ReissueCourseAccessLink;
using HiMentor.IdentidadeAcesso.Application.Queries.GetPlatformStats;
using HiMentor.IdentidadeAcesso.Application.Queries.GetUserCoursesAndAccess;
using HiMentor.IdentidadeAcesso.Application.Queries.ListUsers;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.IdentidadeAcesso.Api.Controllers;

/// <summary>
/// Painel do dono da plataforma: listagem/gestão de usuários (ativar, suspender, excluir) e
/// visão geral de estatísticas. Compartilha o prefixo de rota com RolesController (gestão de
/// roles) — templates diferentes, sem colisão.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminUsersController(ISender sender, ICurrentUserService currentUser) : ControllerBase
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

    /// <summary>
    /// Cursos comprados/matriculados por este usuário, com status de pagamento e de acesso --
    /// usado pela seção "Cursos e acessos" do detalhe do usuário, para o suporte localizar
    /// rapidamente uma compra e (se aplicável) reemitir o link de acesso. Nunca retorna um
    /// token/link pronto -- ver ReissueCourseAccessLink.
    /// </summary>
    [HttpGet("{userId:guid}/courses")]
    public async Task<IActionResult> GetUserCourses(Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new GetUserCoursesAndAccessQuery(userId), ct);
        return Ok(result);
    }

    /// <summary>
    /// Reemite (gera um novo) Magic Link de acesso a um curso específico -- usado quando o
    /// e-mail original de liberação de acesso não chegou ao aluno. Só funciona se o usuário já
    /// tiver acesso liberado (matrícula ativa) a este curso; nunca concede acesso novo.
    /// </summary>
    [HttpPost("{userId:guid}/courses/{courseId:guid}/access-link")]
    public async Task<IActionResult> ReissueCourseAccessLink(Guid userId, Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(
            new ReissueCourseAccessLinkCommand(currentUser.UserId!.Value, userId, courseId), ct);
        return Ok(result);
    }
}
