using HiMentor.IdentidadeAcesso.Application.Common;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password, string? IpAddress = null)
    : IRequest<AuthTokens>;
