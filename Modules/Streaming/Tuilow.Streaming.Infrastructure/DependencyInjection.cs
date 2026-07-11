using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Tuilow.Streaming.Infrastructure.Repositories;
using Tuilow.Streaming.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Streaming.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra repositório e o serviço de streaming (Cloudflare real ou Mock local,
    /// conforme Cloudflare:MockMode). Chamar no Host.
    /// </summary>
    public static IServiceCollection AddStreamingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IVideoRepository, VideoRepository>();

        // Importação de vídeo externo (passo 2 do assistente) — usa oEmbed público do
        // YouTube/Vimeo, real independentemente de Cloudflare:MockMode (não depende do
        // Cloudflare Stream em si, então não faz sentido ter uma versão "mock" separada).
        services.AddHttpClient<IMediaImportService, MediaImportService>();

        var cloudflareMock = configuration.GetValue<bool>("Cloudflare:MockMode");
        if (cloudflareMock)
        {
            // MockStreamingService usa IHttpContextAccessor para montar a URL de upload a
            // partir da requisição atual (scheme+host+porta reais) — registro é idempotente
            // mesmo que outro módulo já tenha chamado AddHttpContextAccessor().
            services.AddHttpContextAccessor();
            services.AddScoped<IStreamingService, MockStreamingService>();
        }
        else
        {
            services.AddHttpClient<IStreamingService, CloudflareStreamService>(client =>
            {
                client.BaseAddress = new Uri("https://api.cloudflare.com");
                var apiToken = configuration["Cloudflare:ApiToken"];
                if (!string.IsNullOrWhiteSpace(apiToken))
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            });
        }

        return services;
    }
}
