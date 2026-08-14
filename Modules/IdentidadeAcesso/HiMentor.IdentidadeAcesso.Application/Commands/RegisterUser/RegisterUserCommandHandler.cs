using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow,
    IEmailService emailService
) : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        // Achado em teste manual: DuplicateEmailException (tipo do próprio Domain) não é
        // reconhecida pelo switch do ExceptionHandlingMiddleware -- caía no bucket genérico
        // ("Ocorreu um erro interno", 500), escondendo do usuário que o problema era só o
        // e-mail já estar cadastrado. BusinessException já é tratada (422, mensagem repassada
        // ao cliente como está) — mesmo padrão usado no resto do módulo.
        if (await userRepository.ExistsByEmailAsync(request.Email, ct))
            throw new BusinessException($"Já existe uma conta com o e-mail {request.Email}. Entre ou recupere sua senha.");

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
