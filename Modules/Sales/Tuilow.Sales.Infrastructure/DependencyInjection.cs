using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using Tuilow.Sales.Infrastructure.Repositories;
using Tuilow.Sales.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tuilow.Sales.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra repositórios e o cliente HTTP do Asaas. Chamar no Host.</summary>
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ICoursePurchaseRepository, CoursePurchaseRepository>();
        services.AddScoped<IUserProvisioningService, IdentidadeAcessoUserProvisioningService>();
        // A5: usado pelo job de reconciliação abaixo para saber se uma compra Confirmed já tem
        // WalletTransaction correspondente no módulo Finance.
        services.AddScoped<IWalletCreditChecker, FinanceWalletCreditChecker>();

        services.AddHttpClient<IPaymentService, AsaasPaymentService>(client =>
            {
                // Achado em teste manual (produção): a Asaas migrou o endereço da API (ver
                // changelog "Novo endereço em produção da nossa API") -- o formato antigo
                // (asaas.com/api/v3/...) ainda "funciona", mas via REDIRECT HTTP pro endereço
                // novo, e o HttpClient do .NET rebaixa POST pra GET ao seguir um redirect
                // 301/302 -- destruía silenciosamente toda chamada de criação (cliente,
                // cobrança, assinatura), que virava uma busca vazia sem erro nenhum. Usa direto
                // o endereço novo (sem /api no caminho, ver chamadas abaixo), que não redireciona.
                var baseUrl = configuration["Asaas:BaseUrl"] ?? "https://api-sandbox.asaas.com/v3";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Add("access_token", configuration["Asaas:ApiKey"]);
                client.DefaultRequestHeaders.Add("User-Agent", "Tuilow/1.0");
            })
            // M2: sem isso, uma instabilidade momentânea da Asaas travava a requisição do
            // comprador por até ~100s (timeout padrão do HttpClient) e nunca era refeita
            // automaticamente. O handler de resiliência padrão do .NET já aplica um conjunto
            // sensato: timeout por tentativa (10s) + retry com backoff exponencial e jitter (3
            // tentativas) + circuit breaker (para de martelar a Asaas se ela já estiver fora do
            // ar) + timeout total da requisição (30s).
            .AddStandardResilienceHandler();

        // B4: job periódico que efetiva PastDue -> Expired (assinatura) e expira compras Pending
        // abandonadas — nenhum dos dois acontecia sozinho antes.
        services.AddHostedService<SalesExpirationBackgroundService>();

        // A5: job periódico de reconciliação Sales × Finance (venda Confirmed sem crédito na
        // carteira do criador) — detecta e loga como crítico, não reprocessa sozinho (ver
        // comentário em FinanceReconciliationBackgroundService sobre o motivo).
        services.AddHostedService<FinanceReconciliationBackgroundService>();

        return services;
    }
}
