using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.RemoveRole;

/// <summary>Revoga um role de um usuário. Apenas Admin pode executar.</summary>
public sealed record RemoveRoleCommand(
    Guid TargetUserId,
    string RoleName
) : IRequest;
