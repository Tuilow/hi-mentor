using Tuilow.IdentidadeAcesso.Application.Common;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? IpAddress = null) : IRequest<AuthTokens>;
