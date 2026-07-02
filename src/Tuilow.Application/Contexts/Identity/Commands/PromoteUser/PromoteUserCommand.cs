using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.PromoteUser;

/// <summary>
/// Atribui um role a um usuário (multi-role: não remove os roles existentes).
/// Apenas Admin pode executar. Use RemoveRoleCommand para revogar um role.
/// </summary>
public sealed record PromoteUserCommand(
    Guid TargetUserId,
    string RoleName
) : IRequest;
