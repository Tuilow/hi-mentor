using Tuilow.Sales.Application;
using Tuilow.Sales.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Sales.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(...).Assembly) no AddControllers().
    /// Recebe IConfiguration porque a Infrastructure precisa configurar o HttpClient do Asaas.
    /// </summary>
    public static IServiceCollection AddSalesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSalesApplication();
        services.AddSalesInfrastructure(configuration);
        return services;
    }
}
