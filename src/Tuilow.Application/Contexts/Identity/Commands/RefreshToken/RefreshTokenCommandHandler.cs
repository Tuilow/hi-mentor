using Tuilow.Application.Common.Exceptions;
using Tuilow.Application.Common.Interfaces;
using Tuilow.Application.Common.Models;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<RefreshTokenCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByRefreshTokenAsync(request.Token, ct)
            ?? throw new UnauthorizedException("Refresh token inválido.");

        var existingToken = user.GetActiveRefreshToken(request.Token)
            ?? throw new UnauthorizedException("Refresh token expirado ou revogado.");

        var newRefreshTokenStr = jwtService.GenerateRefreshToken();
        var newExpires = DateTime.UtcNow.AddDays(30);

        existingToken.Revoke(newRefreshTokenStr);
        user.AddRefreshToken(newRefreshTokenStr, newExpires, request.IpAddress);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);

        var accessToken = jwtService.GenerateAccessToken(user);
        return new AuthTokens(accessToken, newRefreshTokenStr, DateTime.UtcNow.AddMinutes(15), newExpires);
    }
}
