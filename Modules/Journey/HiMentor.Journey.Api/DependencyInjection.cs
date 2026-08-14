using HiMentor.Journey.Application;
using HiMentor.Journey.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Journey.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(...).Assembly) no AddControllers().
    /// </summary>
    public static IServiceCollection AddJourneyModule(this IServiceCollection services)
    {
        services.AddJourneyApplication();
        services.AddJourneyInfrastructure();
        return services;
    }
}
