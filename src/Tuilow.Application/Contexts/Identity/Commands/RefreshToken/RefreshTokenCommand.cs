using Tuilow.Application.Common.Models;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? IpAddress = null) : IRequest<AuthTokens>;
