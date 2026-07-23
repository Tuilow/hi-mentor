using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.SuspendUser;

/// <summary>
/// Suspende uma conta pelo painel do dono da plataforma — bloqueia login e revoga sessões
/// ativas. Diferente de RemoveRoleCommand: não mexe em roles, só no Status da conta.
/// </summary>
public sealed record SuspendUserCommand(Guid TargetUserId) : IRequest;
