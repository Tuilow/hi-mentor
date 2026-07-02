using Tuilow.Streaming.Application;
using Tuilow.Streaming.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Streaming.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(...).Assembly) no AddControllers().
    /// Recebe IConfiguration porque a Infrastructure decide entre Cloudflare real ou Mock.
    /// </summary>
    public static IServiceCollection AddStreamingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStreamingApplication();
        services.AddStreamingInfrastructure(configuration);
        return services;
    }
}
