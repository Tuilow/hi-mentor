using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.IdentidadeAcesso.Domain.Exceptions;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow,
    IEmailService emailService
) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateEmailException(request.Email);

        // Todo novo usuário nasce com o role padrão Student (multi-role: outros
        // roles como Creator/ChannelMember são adicionados depois, sem remover este).
        var studentRole = await roleRepository.GetByNameAsync(RoleNames.Student, ct);
        var user = User.Register(request.Email, request.Password, request.FirstName, request.LastName, studentRole);

        await userRepository.AddAsync(user, ct);
        await uow.SaveChangesAsync(ct);

        // E-mail com o código de confirmação (assíncrono, não bloqueia resposta).
        // IMPORTANTE: usar CancellationToken.None aqui, NUNCA o `ct` da requisição — este envio
        // continua rodando depois que a resposta HTTP já foi devolvida ao cliente, e o `ct` da
        // requisição é cancelado quando a conexão termina (em produção, atrás do proxy/load
        // balancer do Railway isso acontece rápido o suficiente para abortar o handshake SMTP
        // no meio, gerando TaskCanceledException dentro do MailKit ConnectAsync — foi
        // exatamente esse o bug: localmente a conexão ficava aberta tempo suficiente por sorte,
        // em produção não).
        _ = emailService.SendWelcomeAsync(
            user.Id, user.Email.Value, user.Profile.FirstName,
            user.EmailConfirmationToken!, CancellationToken.None);

        // Sprint Item 4: cadastro não faz mais login automático — a conta nasce com status
        // PendingConfirmation (ver User.Register) e só pode ser usada em /auth/login depois que
        // o código acima for confirmado em /auth/confirm-email (ver LoginUserCommandHandler).
        return new RegisterUserResult(user.Id, user.Email.Value);
    }
}
