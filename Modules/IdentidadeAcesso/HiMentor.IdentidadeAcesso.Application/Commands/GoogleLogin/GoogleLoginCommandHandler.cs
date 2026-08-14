using HiMentor.IdentidadeAcesso.Application.Common;
using HiMentor.IdentidadeAcesso.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using Google.Apis.Auth;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<GoogleLoginCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(GoogleLoginCommand request, CancellationToken ct)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
        }
        catch
        {
            throw new UnauthorizedException("Token Google inválido.");
        }

        var user = await userRepository.GetBySocialLoginAsync("Google", payload.Subject, ct);

        if (user is null)
        {
            var existingByEmail = await userRepository.GetByEmailAsync(payload.Email, ct);
            if (existingByEmail is not null)
            {
                // Usuário já existe — vincula a conta Google.
                // AddSocialLogin retorna null se o vínculo já existia.
                var socialLogin = existingByEmail.AddSocialLogin("Google", payload.Subject, payload.Email);
                if (socialLogin is not null)
                    // Força EntityState.Added — sem isso EF Core gera UPDATE via DetectChanges
                    await userRepository.AddSocialLoginAsync(socialLogin, ct);
                user = existingByEmail;
                // NÃO chama Update(user) — usuário já está rastreado pelo DbContext
            }
            else
            {
                // Novo usuário via Google — AddAsync rastreia o grafo inteiro como Added.
                var studentRole = await roleRepository.GetByNameAsync(RoleNames.Student, ct);
                user = User.RegisterFromSocialLogin(
                    payload.Email,
                    payload.GivenName ?? "Usuário",
                    payload.FamilyName ?? string.Empty,
                    "Google", payload.Subject, studentRole);
                await userRepository.AddAsync(user, ct);
            }
        }

        var refreshStr = jwtService.GenerateRefreshToken();
        var newToken = user.AddRefreshToken(refreshStr, DateTime.UtcNow.AddDays(30));
        // Força EntityState.Added — sem isso EF Core gera UPDATE via DetectChanges (Guid não-default)
        await userRepository.AddRefreshTokenAsync(newToken, ct);
        await uow.SaveChangesAsync(ct);

        return new AuthTokens(
            jwtService.GenerateAccessToken(user),
            refreshStr,
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow.AddDays(30));
    }
}
