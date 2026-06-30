using DogMaster.Application.Common.Exceptions;
using DogMaster.Application.Common.Interfaces;
using DogMaster.Application.Common.Models;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Entities;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using Google.Apis.Auth;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.GoogleLogin;

public sealed class GoogleLoginCommandHandler(
    IUserRepository userRepository,
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
                existingByEmail.AddSocialLogin("Google", payload.Subject, payload.Email);
                user = existingByEmail;
                userRepository.Update(user);
            }
            else
            {
                user = User.RegisterFromSocialLogin(
                    payload.Email,
                    payload.GivenName ?? "Usuário",
                    payload.FamilyName ?? string.Empty,
                    "Google", payload.Subject);
                await userRepository.AddAsync(user, ct);
            }
        }

        var refreshStr = jwtService.GenerateRefreshToken();
        user.AddRefreshToken(refreshStr, DateTime.UtcNow.AddDays(30));
        await uow.SaveChangesAsync(ct);

        return new AuthTokens(
            jwtService.GenerateAccessToken(user),
            refreshStr,
            DateTime.UtcNow.AddMinutes(15),
            DateTime.UtcNow.AddDays(30));
    }
}
