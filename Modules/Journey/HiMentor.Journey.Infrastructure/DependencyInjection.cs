using HiMentor.Journey.Domain.Interfaces;
using HiMentor.Journey.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Journey.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Journey. Chamar no Host.</summary>
    public static IServiceCollection AddJourneyInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ILearnerProfileRepository, LearnerProfileRepository>();
        return services;
    }
}
