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

        services.AddHttpClient<IPaymentService, AsaasPaymentService>(client =>
        {
            var baseUrl = configuration["Asaas:BaseUrl"] ?? "https://sandbox.asaas.com";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("access_token", configuration["Asaas:ApiKey"]);
            client.DefaultRequestHeaders.Add("User-Agent", "Tuilow/1.0");
        });

        return services;
    }
}
