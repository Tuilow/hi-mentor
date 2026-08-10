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

        // Onboarding financeiro via subconta Asaas (BaaS) -- substitui, para criadores novos, o
        // fluxo de "cole sua API Key" acima (CreatorAsaasAccount continua registrado só por
        // compatibilidade histórica, ver comentário em CreatorAsaasSubaccount).
        services.AddScoped<ICreatorAsaasSubaccountRepository, CreatorAsaasSubaccountRepository>();
        services.AddScoped<IProcessedAsaasAccountEventRepository, ProcessedAsaasAccountEventRepository>();
        services.AddScoped<IAsaasSubaccountClient, AsaasSubaccountClient>();
        services.AddScoped<IAsaasAccountStatusWebhookAuthenticator, AsaasAccountStatusWebhookAuthenticator>();
        // Diferente do client "AsaasOnboarding" acima (legado, sem resiliência configurada),
        // este aplica o mesmo handler de resiliência padrão já usado em Sales.Infrastructure
        // (timeout por tentativa + retry com backoff/jitter + circuit breaker) -- corrigido aqui
        // de brinde, já que estamos tocando exatamente esta área.
        services.AddHttpClient("AsaasSubaccount").AddStandardResilienceHandler();

        return services;
    }
}
