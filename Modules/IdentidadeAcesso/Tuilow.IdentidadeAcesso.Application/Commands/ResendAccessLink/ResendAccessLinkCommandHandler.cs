using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ResendAccessLink;

public sealed class ResendAccessLinkCommandHandler(
    IUserRepository userRepository, IUnitOfWork uow, IEmailService emailService
) : IRequestHandler<ResendAccessLinkCommand>
{
    public async Task Handle(ResendAccessLinkCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct);
        if (user is null) return; // Não revela se e-mail existe (mesmo padrão de ForgotPasswordCommandHandler)

        // Mesma construção de token usada em IdentidadeAcessoMagicLinkIssuer (dois GUIDs opacos
        // concatenados) — reimplementada aqui em vez de referenciar Tuilow.Learning.Application
        // porque IMagicLinkIssuer vive naquele módulo e IdentidadeAcesso não depende de Learning
        // (dependência estritamente na direção contrária em todo o resto do código).
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var magicLink = user.IssueMagicLink(token);
        await userRepository.AddMagicLinkTokenAsync(magicLink, ct);
        await uow.SaveChangesAsync(ct);

        await emailService.SendAccessLinkAsync(user.Email.Value, user.Profile.FirstName, token, ct);
    }
}
