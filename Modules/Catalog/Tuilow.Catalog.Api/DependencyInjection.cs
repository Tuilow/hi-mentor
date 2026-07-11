using Tuilow.Catalog.Application;
using Tuilow.Catalog.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Catalog.Api;

public static class DependencyInjection
{
    /// <summary>
    /// Registra Application + Infrastructure do módulo. Chamar no Host junto com
    /// AddApplicationPart(typeof(DependencyInjection).Assembly) no AddControllers()
    /// para o Host descobrir os Controllers deste módulo.
    /// </summary>
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddCatalogApplication();
        services.AddCatalogInfrastructure();
        return services;
    }
}
