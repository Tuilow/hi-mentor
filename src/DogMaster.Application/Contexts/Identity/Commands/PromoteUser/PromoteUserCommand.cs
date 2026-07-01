using DogMaster.Domain.Contexts.Identity.Enums;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.PromoteUser;

/// <summary>
/// Altera o role de um usuário. Apenas Admin pode executar.
/// </summary>
public sealed record PromoteUserCommand(
    Guid TargetUserId,
    UserRole NewRole
) : IRequest;
