using DogMaster.Application.Common.Models;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName
) : IRequest<AuthTokens>;
