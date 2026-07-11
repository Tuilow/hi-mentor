using Tuilow.Finance.Application;
using Tuilow.Finance.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Finance.Api;

public static class DependencyInjection
{
    /// <summary>Registra Application + Infrastructure do módulo Finance. Chamar no Host.</summary>
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddFinanceApplication();
        services.AddFinanceInfrastructure();
        return services;
    }
}
