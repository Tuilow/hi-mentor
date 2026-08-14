using HiMentor.CreatorStudio.Application;
using HiMentor.CreatorStudio.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.CreatorStudio.Api;

public static class DependencyInjection
{
    /// <summary>Registra Application + Infrastructure do módulo CreatorStudio. Chamar no Host.</summary>
    public static IServiceCollection AddCreatorStudioModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCreatorStudioApplication();
        services.AddCreatorStudioInfrastructure(configuration);
        return services;
    }
}
