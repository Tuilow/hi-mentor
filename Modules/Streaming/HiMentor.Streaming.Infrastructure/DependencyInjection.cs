using HiMentor.Streaming.Application.Interfaces;
using HiMentor.Streaming.Domain.Interfaces;
using HiMentor.Streaming.Infrastructure.BackgroundJobs;
using HiMentor.Streaming.Infrastructure.Repositories;
using HiMentor.Streaming.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Streaming.Infrastructure;

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

                // Timeout padrão do HttpClientFactory é 100s — curto demais pro upload direto
                // (UploadFileAsync) de um vídeo baixado do YouTube, que pode ter várias
                // centenas de MB. Só afeta esse client (Streaming), não o resto da aplicação.
                client.Timeout = TimeSpan.FromMinutes(30);
            });
        }

        // Fila (em memória) + worker do "baixar vídeo do YouTube e hospedar no Cloudflare
        // Stream" — YouTubeDownloadQueue precisa ser singleton (mesma instância entre quem
        // escreve — o handler de importação, por requisição — e quem lê — o worker).
        services.AddSingleton<YouTubeDownloadQueue>();
        services.AddSingleton<IYouTubeDownloadQueue>(sp => sp.GetRequiredService<YouTubeDownloadQueue>());
        services.AddHostedService<YouTubeDownloadWorker>();

        return services;
    }
}
