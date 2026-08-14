using HiMentor.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace HiMentor.SharedKernel.Infrastructure.WhatsApp;

/// <summary>
/// Implementação no-op de <see cref="IWhatsAppService"/> — não envia nada de verdade, só loga.
/// Existe para o fluxo de "acesso liberado" já chamar a porta de WhatsApp desde já (opcional,
/// best-effort, nunca bloqueia o e-mail nem a liberação de acesso), sem exigir credencial de
/// provedor agora. Trocar por uma implementação real (Twilio/Z-API/WhatsApp Business API) é só
/// questão de registrar outra classe em DependencyInjection.AddSharedKernel.
/// </summary>
public sealed class NoOpWhatsAppService(ILogger<NoOpWhatsAppService> logger) : IWhatsAppService
{
    public Task SendCourseAccessGrantedAsync(
        string phoneNumber, string firstName, string courseTitle, string magicLinkUrl, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[WhatsApp não enviado — nenhum provedor configurado] Para {Phone} ({Name}): acesso liberado a '{Course}'. Link: {Link}",
            phoneNumber, firstName, courseTitle, magicLinkUrl);
        return Task.CompletedTask;
    }
}
