using HiMentor.Catalog.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Catalog.Infrastructure.Repositories;
using HiMentor.Catalog.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HiMentor.Catalog.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Catalog. Chamar no Host.</summary>
    public static IServiceCollection AddCatalogInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IInstructorLookup, IdentidadeAcessoInstructorLookup>();
        // Onboarding financeiro (subconta Asaas/BaaS) -- ver PublishCourseCommandHandler.
        services.AddScoped<ICreatorFinancialStatusLookup, FinanceCreatorFinancialStatusLookup>();
        return services;
    }
}
