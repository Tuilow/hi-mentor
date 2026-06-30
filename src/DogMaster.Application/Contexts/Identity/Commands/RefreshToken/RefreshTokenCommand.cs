using DogMaster.Application.Common.Models;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token, string? IpAddress = null) : IRequest<AuthTokens>;
