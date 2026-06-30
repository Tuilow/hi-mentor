using DogMaster.Application.Common.Models;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password, string? IpAddress = null)
    : IRequest<AuthTokens>;
