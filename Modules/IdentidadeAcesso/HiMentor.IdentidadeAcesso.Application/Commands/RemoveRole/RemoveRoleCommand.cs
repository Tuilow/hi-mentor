using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.RemoveRole;

/// <summary>Revoga um role de um usuário. Apenas Admin pode executar.</summary>
public sealed record RemoveRoleCommand(
    Guid TargetUserId,
    string RoleName
) : IRequest;
