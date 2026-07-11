using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Finance.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Finance.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios do módulo Finance. Chamar no Host.</summary>
    public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICreatorWalletRepository, CreatorWalletRepository>();
        services.AddScoped<IPlatformFeeConfigurationRepository, PlatformFeeConfigurationRepository>();
        return services;
    }
}
