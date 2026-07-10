using Tuilow.Channel.Application;
using Tuilow.Channel.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Channel.Api;

public static class DependencyInjection
{
    /// <summary>Registra Application + Infrastructure do módulo Channel. Chamar no Host.</summary>
    public static IServiceCollection AddChannelModule(this IServiceCollection services)
    {
        services.AddChannelApplication();
        services.AddChannelInfrastructure();
        return services;
    }
}
