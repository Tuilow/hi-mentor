using Tuilow.IdentidadeAcesso.Application.Common;
using Tuilow.IdentidadeAcesso.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ConsumeMagicLink;

public sealed class ConsumeMagicLinkCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<ConsumeMagicLinkCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(ConsumeMagicLinkCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByMagicLinkTokenAsync(request.Token, ct)
            ?? throw new UnauthorizedException("Link de acesso inválido ou expirado.");

        // Lança InvalidOperationException (422) se já foi usado ou expirou — token existe mas não é mais válido.
        user.ConsumeMagicLink(request.Token);

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
