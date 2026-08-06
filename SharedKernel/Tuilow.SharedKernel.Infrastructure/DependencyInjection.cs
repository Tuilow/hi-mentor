using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.SharedKernel.Infrastructure.Clock;
using Tuilow.SharedKernel.Infrastructure.Email;
using Tuilow.SharedKernel.Infrastructure.Frontend;
using Tuilow.SharedKernel.Infrastructure.Security;
using Tuilow.SharedKernel.Infrastructure.WhatsApp;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.SharedKernel.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os serviços comuns do SharedKernel. Chamar no Host antes dos módulos.</summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        // IHttpClientFactory usado pelo EmailService para chamar a API HTTP do Mailgun.
        services.AddHttpClient();
        services.AddScoped<IEmailService, EmailService>();
        // Monta URLs absolutas do frontend (ex.: link de Magic Link reemitido pelo painel
        // administrativo) sem duplicar a leitura de "FrontendUrl" em cada modulo -- ver
        // IFrontendUrlProvider.
        services.AddSingleton<IFrontendUrlProvider, FrontendUrlProvider>();
        // Sem provedor configurado ainda — troque por uma implementação real quando houver
        // credencial (Twilio/Z-API/WhatsApp Business API). Ver comentário em IWhatsAppService.
        services.AddScoped<IWhatsAppService, NoOpWhatsAppService>();

        // Data Protection: usado por ISecretProtector para proteger a API Key da conta Asaas
        // externa de cada creator (marketplace de split). O provider (AddDataProtection) e
        // registrado no Host (Program.cs), com as chaves persistidas no Postgres -- aqui so
        // registramos o wrapper que os modulos consomem via ISecretProtector.
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
        return services;
    }
}
