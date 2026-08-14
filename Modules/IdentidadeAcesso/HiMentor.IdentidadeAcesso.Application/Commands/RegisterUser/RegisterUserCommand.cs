using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<RegisterUserResult>;
