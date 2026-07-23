using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.DeleteUser;

/// <summary>
/// Exclusão de conta pelo painel do dono da plataforma. Soft-delete no usuário (preserva
/// histórico financeiro/fiscal), mas apaga de verdade todos os vídeos do criador (Cloudflare +
/// banco) e arquiva todos os cursos dele.
/// </summary>
public sealed record DeleteUserCommand(Guid TargetUserId) : IRequest;
