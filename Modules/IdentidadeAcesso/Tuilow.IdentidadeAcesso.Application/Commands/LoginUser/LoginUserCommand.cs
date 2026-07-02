using Tuilow.IdentidadeAcesso.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password, string? IpAddress = null)
    : IRequest<AuthTokens>;
