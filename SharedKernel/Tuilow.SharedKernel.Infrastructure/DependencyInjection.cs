using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.SharedKernel.Infrastructure.Clock;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.SharedKernel.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os serviços comuns do SharedKernel. Chamar no Host antes dos módulos.</summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        return services;
    }
}
