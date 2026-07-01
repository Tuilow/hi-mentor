using DogMaster.Domain.Contexts.Subscription.Entities;
using DogMaster.Domain.Contexts.Subscription.Enums;
using DogMaster.Domain.Contexts.Subscription.Interfaces;
using DogMaster.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DogMaster.Infrastructure.Repositories;

public sealed class SubscriptionRepository(ApplicationDbContext context) : ISubscriptionRepository
{
    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Subscriptions.Include(s => s.Payments).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IEnumerable<Subscription>> GetAllAsync(CancellationToken ct = default) =>
        await context.Subscriptions.ToListAsync(ct);

    public async Task AddAsync(Subscription entity, CancellationToken ct = default) =>
        await context.Subscriptions.AddAsync(entity, ct);

    public void Update(Subscription entity) => context.Subscriptions.Update(entity);
    public void Delete(Subscription entity) => context.Subscriptions.Remove(entity);

    public async Task<Subscription?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Subscriptions
            .Include(s => s.Payments)
            .Where(s => s.UserId == userId &&
                (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial))
            .OrderByDescending(s => s.CurrentPeriodEnd)
            .FirstOrDefaultAsync(ct);

    public async Task<Subscription?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default) =>
        await context.Subscriptions
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.AsaasSubscriptionId == asaasId, ct);

    public async Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default) =>
        await context.Plans.Include(p => p.Features).FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default) =>
        await context.Plans.Include(p => p.Features).Where(p => p.IsActive).ToListAsync(ct);
}
