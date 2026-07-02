using Tuilow.Application.Common.Interfaces;
using Tuilow.Application.Common.Models;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Entities;
using Tuilow.Domain.Contexts.Identity.Enums;
using Tuilow.Domain.Contexts.Identity.Exceptions;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow,
    IJwtService jwtService,
    IEmailService emailService
) : IRequestHandler<RegisterUserCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateEmailException(request.Email);

        // Todo novo usuário nasce com o role padrão Student (multi-role: outros
        // roles como Creator/ChannelMember são adicionados depois, sem remover este).
        var studentRole = await roleRepository.GetByNameAsync(RoleNames.Student, ct);
        var user = User.Register(request.Email, request.Password, request.FirstName, request.LastName, studentRole);

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
