using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.SharedKernel.Infrastructure.Clock;
using Tuilow.SharedKernel.Infrastructure.Email;
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
        // Sem provedor configurado ainda — troque por uma implementação real quando houver
        // credencial (Twilio/Z-API/WhatsApp Business API). Ver comentário em IWhatsAppService.
        services.AddScoped<IWhatsAppService, NoOpWhatsAppService>();
        return services;
    }
}
