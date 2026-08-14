using HiMentor.IdentidadeAcesso.Application.Common;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? IpAddress = null) : IRequest<AuthTokens>;
