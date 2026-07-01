using DogMaster.Application.Common.Exceptions;
using DogMaster.Application.Common.Interfaces;
using DogMaster.Application.Common.Models;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.RefreshToken;

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
