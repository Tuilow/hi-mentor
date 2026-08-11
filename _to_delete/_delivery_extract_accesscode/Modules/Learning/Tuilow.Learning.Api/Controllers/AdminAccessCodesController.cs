using Tuilow.Learning.Application.Commands.GenerateAccessCode;
using Tuilow.Learning.Application.Queries.GetAccessCodesAdmin;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Learning.Api.Controllers;

/// <summary>
/// Painel do dono da plataforma ("Plataforma" no menu) — gera e lista códigos de acesso para
/// qualquer programa publicado. Só Admin (não Creator/Mentor): não existe emissão de código pelo
/// próprio criador nesta primeira versão.
/// </summary>
[ApiController]
[Route("api/v1/admin/access-codes")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminAccessCodesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Gera um novo código de acesso para um programa publicado.</summary>
    [HttpPost]
    public async Task<IActionResult> Generate([FromBody] GenerateAccessCodeRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new GenerateAccessCodeCommand(
            currentUser.UserId!.Value, request.CourseId, request.MaxUses, request.ExpiresAt), ct);
        return Ok(result);
    }

    /// <summary>Lista todos os códigos de acesso já emitidos na plataforma.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await sender.Send(new GetAccessCodesAdminQuery(), ct);
        return Ok(result);
    }
}

public sealed record GenerateAccessCodeRequest(Guid CourseId, int? MaxUses, DateTime? ExpiresAt);
