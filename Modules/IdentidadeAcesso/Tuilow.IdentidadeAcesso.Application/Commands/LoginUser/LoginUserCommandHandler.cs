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

        // Sprint Item 4: cadastro não faz mais login automático — a conta só sai de
        // PendingConfirmation depois que o código enviado por e-mail é confirmado em
        // /auth/confirm-email (ver User.Register / User.ConfirmEmail). Verificado só depois da
        // senha estar correta, mesmo padrão do check de Suspended acima, pra não vazar o status
        // da conta pra quem só está tentando adivinhar a senha.
        if (user.Status == UserStatus.PendingConfirmation)
            throw new UnauthorizedException("Confirme seu e-mail antes de entrar. Enviamos um código de confirmação para o seu e-mail.");

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
