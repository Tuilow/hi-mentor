using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Finance.Infrastructure.Repositories;
using Tuilow.Finance.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Finance.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra os repositórios e serviços do módulo Finance. Chamar no Host.</summary>
    public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ICreatorWalletRepository, CreatorWalletRepository>();
        services.AddScoped<IPlatformFeeConfigurationRepository, PlatformFeeConfigurationRepository>();
        // Marketplace de split (creator como emissor da cobranca) -- ver CreatorAsaasAccount.
        services.AddScoped<ICreatorAsaasAccountRepository, CreatorAsaasAccountRepository>();
        services.AddScoped<ICreatorAsaasCustomerRepository, CreatorAsaasCustomerRepository>();
        services.AddScoped<IAsaasAccountOnboardingService, AsaasAccountOnboardingService>();
        services.AddHttpClient("AsaasOnboarding");
        services.AddScoped<ICreatorDisplayInfoLookup, IdentidadeAcessoCreatorDisplayInfoLookup>();
        return services;
    }
}
