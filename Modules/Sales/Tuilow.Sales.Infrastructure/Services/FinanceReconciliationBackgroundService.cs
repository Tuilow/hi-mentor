using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>
/// Achado A5 da avaliação: mesma causa-raiz do C2 (domain events publicados de forma síncrona
/// logo após o commit, sem Outbox nem fila), aplicada especificamente ao módulo Finance — se o
/// handler que credita a carteira do criador falhar depois do commit da venda (deadlock,
/// indisponibilidade momentânea do banco), a compra fica Confirmed em Sales, mas nenhuma
/// WalletTransaction é criada, sem NENHUM alerta além de um log e sem mecanismo de reconciliação.
///
/// Este job compara periodicamente CoursePurchase.Confirmed × crédito correspondente na carteira
/// (via IWalletCreditChecker, que abstrai o módulo Finance) e emite um log crítico para cada
/// divergência encontrada — deliberadamente NÃO reprocessa sozinho: reprocessar chamaria de novo
/// TODOS os handlers deste evento (Learning inclusive), o que reenviaria e-mail de acesso
/// liberado/magic link para um aluno que já foi corretamente avisado — um efeito colateral pior
/// que o log ficar parado esperando alguém olhar. A correção já existe e é segura de chamar
/// (idempotente): POST /api/v1/admin/sales/course-purchases/{id}/reprocess (achado C2 desta
/// mesma auditoria) — o log abaixo traz o CoursePurchaseId pronto para isso.
/// </summary>
public sealed class FinanceReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FinanceReconciliationBackgroundService> logger
) : BackgroundService
{
    // Dá tempo do fluxo normal (SaveChanges -> DispatchDomainEventsAsync -> Finance) terminar
    // antes de considerar uma venda "sem crédito" — evita alertar por uma venda que ainda está
    // sendo processada no exato momento da varredura.
    private readonly TimeSpan _gracePeriod =
        TimeSpan.FromMinutes(configuration.GetValue("Sales:ReconciliationGraceMinutes", 30));

    // Não reescaneia o histórico inteiro a cada execução — só a janela recente.
    private readonly TimeSpan _lookbackWindow =
        TimeSpan.FromDays(configuration.GetValue("Sales:ReconciliationLookbackDays", 30));

    private readonly TimeSpan _interval =
        TimeSpan.FromHours(configuration.GetValue("Sales:ReconciliationJobIntervalHours", 6));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
            var coursePurchaseRepository = scope.ServiceProvider.GetRequiredService<ICoursePurchaseRepository>();
            var walletCreditChecker = scope.ServiceProvider.GetRequiredService<IWalletCreditChecker>();

            var now = DateTime.UtcNow;
            var candidates = await coursePurchaseRepository.GetConfirmedForReconciliationAsync(
                lookbackFloor: now - _lookbackWindow, graceThreshold: now - _gracePeriod, ct);

            var missingCreditCount = 0;
            foreach (var purchase in candidates)
            {
                if (await walletCreditChecker.HasCreditForPurchaseAsync(purchase.Id, ct))
                    continue;

                missingCreditCount++;
                logger.LogCritical(
                    "Reconciliação Finance (achado A5): compra {PurchaseId} está Confirmed desde " +
                    "{ConfirmedAt:u} (criador {CreatorId}, valor {Amount}) mas não tem WalletTransaction " +
                    "correspondente — o criador não foi pago por esta venda. Reprocessar via " +
                    "POST /api/v1/admin/sales/course-purchases/{PurchaseId}/reprocess.",
                    purchase.Id, purchase.ConfirmedAt, purchase.CreatorId, purchase.Amount, purchase.Id);
            }

            if (missingCreditCount > 0)
            {
                logger.LogWarning(
                    "Job de reconciliação Finance: {Count} compra(s) Confirmed sem crédito na carteira do criador.",
                    missingCreditCount);
            }
        }
        catch (Exception ex)
        {
            // Uma falha nesta execução não deve derrubar o BackgroundService inteiro — ele
            // continua rodando normalmente na próxima janela.
            logger.LogError(ex, "Falha ao rodar o job de reconciliação Finance × Sales.");
        }
    }
}
