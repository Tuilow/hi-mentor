using Tuilow.Catalog.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Catalog.Infrastructure.Repositories;
using Tuilow.Catalog.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Catalog. Chamar no Host.</summary>
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IInstructorLookup, IdentidadeAcessoInstructorLookup>();
        return services;
    }
}
