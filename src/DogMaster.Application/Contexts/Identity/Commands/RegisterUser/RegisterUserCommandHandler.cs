using DogMaster.Application.Common.Interfaces;
using DogMaster.Application.Common.Models;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Entities;
using DogMaster.Domain.Contexts.Identity.Exceptions;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow,
    IJwtService jwtService,
    IEmailService emailService
) : IRequestHandler<RegisterUserCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateEmailException(request.Email);

        var user = User.Register(request.Email, request.Password, request.FirstName, request.LastName);

        // Gera refresh token ANTES do AddAsync para EF Core rastrear tudo em uma única unidade
        var refreshTokenStr = jwtService.GenerateRefreshToken();
        var refreshTokenExpires = DateTime.UtcNow.AddDays(30);
        user.AddRefreshToken(refreshTokenStr, refreshTokenExpires);

        await userRepository.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        // E-mail de confirmação (assíncrono, não bloqueia resposta)
        _ = emailService.SendWelcomeAsync(
            user.Email.Value, user.Profile.FirstName,
            user.EmailConfirmationToken!, ct);

        var accessToken = jwtService.GenerateAccessToken(user);
        return new AuthTokens(
            accessToken, refreshTokenStr,
            DateTime.UtcNow.AddMinutes(15), refreshTokenExpires);
    }
}
