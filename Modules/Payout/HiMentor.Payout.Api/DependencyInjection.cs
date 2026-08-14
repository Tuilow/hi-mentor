using HiMentor.Payout.Application;
using HiMentor.Payout.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Payout.Api;

public static class DependencyInjection
{
    /// <summary>Registra Application + Infrastructure do módulo Payout. Chamar no Host.</summary>
    public static IServiceCollection AddPayoutModule(this IServiceCollection services)
    {
        services.AddPayoutApplication();
        services.AddPayoutInfrastructure();
        return services;
    }
}
