using Tuilow.Sales.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>
/// Job periódico que efetiva dois estados que antes nunca eram atribuídos automaticamente
/// (achado B4 da auditoria):
///   - Assinatura em PastDue há mais que o prazo de tolerância -> Expired.
///   - Compra avulsa (CoursePurchase) ainda Pending, criada há mais que o prazo de tolerância
///     (aluno abandonou o checkout, ou a Asaas nunca confirmou) -> Failed.
/// Sem isso, uma assinatura em PastDue ficava nesse estado para sempre e uma compra Pending
/// nunca expirava sozinha.
/// </summary>
public sealed class SalesExpirationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SalesExpirationBackgroundService> logger
) : BackgroundService
{
    // Dá chance de a Asaas tentar cobrar de novo (dunning) antes de cortar o acesso de vez.
    private readonly TimeSpan _pastDueGracePeriod =
        TimeSpan.FromDays(configuration.GetValue("Sales:PastDueGracePeriodDays", 7));

    // Depois disso, uma compra iniciada e nunca paga deixa de ser considerada "em aberto".
    private readonly TimeSpan _pendingPurchaseTimeout =
        TimeSpan.FromHours(configuration.GetValue("Sales:PendingPurchaseTimeoutHours", 24));

    private readonly TimeSpan _interval =
        TimeSpan.FromHours(configuration.GetValue("Sales:ExpirationJobIntervalHours", 1));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Roda uma vez já na subida — não faz sentido deixar assinaturas/compras vencidas
        // esperando até 1h pela primeira varredura.
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var coursePurchaseRepository = scope.ServiceProvider.GetRequiredService<ICoursePurchaseRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var now = DateTime.UtcNow;

            var pastDueSubscriptions = (await subscriptionRepository
                .GetPastDueOlderThanAsync(now - _pastDueGracePeriod, ct)).ToList();
            foreach (var subscription in pastDueSubscriptions)
            {
                subscription.Expire();
                subscriptionRepository.Update(subscription);
            }

            var stalePurchases = (await coursePurchaseRepository
                .GetPendingOlderThanAsync(now - _pendingPurchaseTimeout, ct)).ToList();
            foreach (var purchase in stalePurchases)
            {
                purchase.MarkFailed();
                coursePurchaseRepository.Update(purchase);
            }

            if (pastDueSubscriptions.Count > 0 || stalePurchases.Count > 0)
            {
                await uow.SaveChangesAsync(ct);
                logger.LogInformation(
                    "Job de expiração: {SubscriptionCount} assinatura(s) expirada(s), {PurchaseCount} compra(s) pendente(s) expirada(s).",
                    pastDueSubscriptions.Count, stalePurchases.Count);
            }
        }
        catch (Exception ex)
        {
            // Uma falha nesta execução não deve derrubar o BackgroundService inteiro — ele
            // continua rodando normalmente na próxima janela.
            logger.LogError(ex, "Falha ao rodar o job de expiração de assinaturas/compras pendentes.");
        }
    }
}
