using Tuilow.Learning.Application;
using Tuilow.Learning.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Learning.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(...).Assembly) no AddControllers().
    /// </summary>
    public static IServiceCollection AddLearningModule(this IServiceCollection services)
    {
        services.AddLearningApplication();
        services.AddLearningInfrastructure();
        return services;
    }
}
