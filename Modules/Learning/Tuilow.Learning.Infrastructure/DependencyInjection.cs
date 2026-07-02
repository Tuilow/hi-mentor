using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using Tuilow.Learning.Infrastructure.Repositories;
using Tuilow.Learning.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Learning.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios/serviços do módulo Learning. Chamar no Host.</summary>
    public static IServiceCollection AddLearningInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ICourseAccessChecker, SalesCourseAccessChecker>();
        return services;
    }
}
