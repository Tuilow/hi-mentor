using Tuilow.Application.Common.Models;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password, string? IpAddress = null)
    : IRequest<AuthTokens>;
