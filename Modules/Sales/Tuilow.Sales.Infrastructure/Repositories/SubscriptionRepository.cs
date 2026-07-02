using Tuilow.Sales.Domain.Entities;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using SubscriptionEntity = Tuilow.Sales.Domain.Entities.Subscription;

namespace Tuilow.Sales.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class SubscriptionRepository(DbContext context) : ISubscriptionRepository
{
    public async Task<SubscriptionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>().Include(s => s.Payments).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IEnumerable<SubscriptionEntity>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>().ToListAsync(ct);

    public async Task AddAsync(SubscriptionEntity entity, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>().AddAsync(entity, ct);

    public void Update(SubscriptionEntity entity) => context.Set<SubscriptionEntity>().Update(entity);
    public void Delete(SubscriptionEntity entity) => context.Set<SubscriptionEntity>().Remove(entity);

    public async Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>()
            .Include(s => s.Payments)
            .Where(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.CurrentPeriodEnd)
            .FirstOrDefaultAsync(ct);

    public async Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>()
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.AsaasSubscriptionId == asaasId, ct);

    public async Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default) =>
        await context.Set<Plan>().Include(p => p.Features).FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default) =>
        await context.Set<Plan>().Include(p => p.Features).Where(p => p.IsActive).ToListAsync(ct);

    /// <summary>
    /// Registra o SubscriptionPayment explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified.
    /// </summary>
    public async Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default) =>
        await context.Set<SubscriptionPayment>().AddAsync(payment, ct);
}
