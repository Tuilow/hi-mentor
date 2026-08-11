using MediatR;

namespace Tuilow.Learning.Application.Commands.GenerateAccessCode;

/// <summary>
/// Emissão de código de acesso pelo painel Admin da plataforma (ver AdminAccessCodesController,
/// Authorize Roles=Admin) — só o dono da plataforma emite códigos nesta primeira versão; não foi
/// pedida uma tela de emissão para o próprio Creator/Mentor.
/// </summary>
public sealed record GenerateAccessCodeCommand(
    Guid AdminUserId, Guid CourseId, int? MaxUses, DateTime? ExpiresAt
) : IRequest<GenerateAccessCodeResult>;

public sealed record GenerateAccessCodeResult(
    Guid Id, string Code, string CourseTitle, int? MaxUses, DateTime? ExpiresAt, DateTime CreatedAt
);
