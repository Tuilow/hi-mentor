using Tuilow.IdentidadeAcesso.Application.Common;
using Tuilow.IdentidadeAcesso.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.LoginUser;

public sealed class LoginUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<LoginUserCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Credenciais inválidas.");

        if (!user.ValidatePassword(request.Password))
            throw new UnauthorizedException("Credenciais inválidas.");

        if (user.Status == UserStatus.Suspended)
            throw new UnauthorizedException("Conta suspensa. Entre em contato com o suporte.");

        var refreshTokenStr = jwtService.GenerateRefreshToken();
        var refreshTokenExpires = DateTime.UtcNow.AddDays(30);
        var newToken = user.AddRefreshToken(refreshTokenStr, refreshTokenExpires, request.IpAddress);
        // Força tracking como Added — sem isso EF Core gera UPDATE (Guid não-default)
        await userRepository.AddRefreshTokenAsync(newToken, ct);
        await uow.SaveChangesAsync(ct);

        var accessToken = jwtService.GenerateAccessToken(user);
        return new AuthTokens(
            accessToken, refreshTokenStr,
            DateTime.UtcNow.AddMinutes(15), refreshTokenExpires);
    }
}
