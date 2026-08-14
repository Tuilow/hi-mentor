using HiMentor.CreatorStudio.Application.Interfaces;
using HiMentor.CreatorStudio.Domain.Interfaces;
using HiMentor.CreatorStudio.Infrastructure.Repositories;
using HiMentor.CreatorStudio.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.CreatorStudio.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra repositório e o gerador de conteúdo por IA (mock local ou provedor real,
    /// conforme AiContentGenerator:MockMode — mesmo padrão de Cloudflare:MockMode do módulo
    /// Streaming). Chamar no Host.
    /// </summary>
    public static IServiceCollection AddCreatorStudioInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ICreatorStyleProfileRepository, CreatorStyleProfileRepository>();
        services.AddScoped<ILessonScriptRepository, LessonScriptRepository>();
        services.AddScoped<IRecordingTemplateRepository, RecordingTemplateRepository>();

        // Porta preparada, sem processamento real ainda (ver NoOpVideoEditingService).
        services.AddSingleton<IVideoEditingService, NoOpVideoEditingService>();

        // Default true: funciona de ponta a ponta sem nenhuma chave de API configurada.
        var aiMock = configuration.GetValue("AiContentGenerator:MockMode", defaultValue: true);
        if (aiMock)
        {
            services.AddSingleton<IAiContentGenerator, MockAiContentGenerator>();
        }
        else
        {
            services.AddHttpClient<IAiContentGenerator, OpenAiContentGenerator>(client =>
            {
                var baseUrl = configuration["AiContentGenerator:BaseUrl"] ?? "https://api.openai.com";
                client.BaseAddress = new Uri(baseUrl);
            });
        }

        return services;
    }
}
