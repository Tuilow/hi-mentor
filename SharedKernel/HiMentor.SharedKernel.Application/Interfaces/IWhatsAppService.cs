namespace HiMentor.SharedKernel.Application.Interfaces;

/// <summary>
/// Porta para envio de mensagens de WhatsApp. Hoje só existe a implementação no-op
/// (<c>NoOpWhatsAppService</c>, em SharedKernel.Infrastructure) registrada, já que a plataforma
/// não tem credencial de nenhum provedor (Twilio, Z-API, WhatsApp Business API...) configurada.
/// O ponto de chamada (CoursePurchaseConfirmedEventHandler, ao lado do e-mail de Magic Link) já
/// está pronto — quando houver um provedor, basta trocar o registro em DependencyInjection por
/// uma implementação real, sem tocar nos módulos que consomem esta interface.
/// </summary>
public interface IWhatsAppService
{
    Task SendCourseAccessGrantedAsync(
        string phoneNumber, string firstName, string courseTitle, string magicLinkUrl, CancellationToken ct = default);
}
