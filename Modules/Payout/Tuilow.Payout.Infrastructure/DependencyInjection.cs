using Tuilow.Payout.Domain.Interfaces;
using Tuilow.Payout.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Payout.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Payout. Chamar no Host.</summary>
    public static IServiceCollection AddPayoutInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPayoutRequestRepository, PayoutRequestRepository>();
        return services;
    }
}
