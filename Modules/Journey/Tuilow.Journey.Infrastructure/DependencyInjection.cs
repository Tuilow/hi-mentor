using Tuilow.Journey.Domain.Interfaces;
using Tuilow.Journey.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Journey.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Journey. Chamar no Host.</summary>
    public static IServiceCollection AddJourneyInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILearnerProfileRepository, LearnerProfileRepository>();
        return services;
    }
}
