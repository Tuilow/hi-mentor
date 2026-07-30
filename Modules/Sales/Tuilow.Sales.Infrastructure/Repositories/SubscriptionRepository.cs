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

    // Inclui assinaturas Cancelled cujo período pago (CurrentPeriodEnd) ainda não passou: o
    // cancelamento não revoga o acesso na hora, só impede a renovação — ver Subscription.IsActive,
    // que aplica exatamente o mesmo critério em memória depois de carregada. Sem isso aqui, a
    // consulta nunca devolveria a assinatura cancelada, tornando o campo IsActive irrelevante.
    public async Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default) =>
        await (
            from s in context.Set<SubscriptionEntity>().Include(s => s.Payments)
            join p in context.Set<Plan>() on s.PlanId equals p.Id
            where s.UserId == userId
                && p.CourseId == null // só plano da plataforma — plano por produto não conta aqui
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial
                    || (s.Status == SubscriptionStatus.Cancelled && s.CurrentPeriodEnd > DateTime.UtcNow))
            orderby s.CurrentPeriodEnd descending
            select s
        ).FirstOrDefaultAsync(ct);

    public async Task<SubscriptionEntity?> GetActiveByUserForCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await (
            from s in context.Set<SubscriptionEntity>().Include(s => s.Payments)
            join p in context.Set<Plan>() on s.PlanId equals p.Id
            where s.UserId == userId
                && p.CourseId == courseId
                && (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial
                    || (s.Status == SubscriptionStatus.Cancelled && s.CurrentPeriodEnd > DateTime.UtcNow))
            orderby s.CurrentPeriodEnd descending
            select s
        ).FirstOrDefaultAsync(ct);

    // B4: usado pelo job periódico que efetiva PastDue → Expired após o prazo de tolerância.
    public async Task<IEnumerable<SubscriptionEntity>> GetPastDueOlderThanAsync(DateTime threshold, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>()
            .Where(s => s.Status == SubscriptionStatus.PastDue && s.UpdatedAt < threshold)
            .ToListAsync(ct);

    public async Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default) =>
        await context.Set<SubscriptionEntity>()
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.AsaasSubscriptionId == asaasId, ct);

    public async Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default) =>
        await context.Set<Plan>().Include(p => p.Features).FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default) =>
        await context.Set<Plan>().Include(p => p.Features).Where(p => p.IsActive && p.CourseId == null).ToListAsync(ct);

    public async Task<IEnumerable<Plan>> GetPlansByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Set<Plan>().Include(p => p.Features).Where(p => p.CourseId == courseId).ToListAsync(ct);

    public async Task AddPlanAsync(Plan plan, CancellationToken ct = default) =>
        await context.Set<Plan>().AddAsync(plan, ct);

    /// <summary>
    /// Registra o SubscriptionPayment explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified.
    /// </summary>
    public async Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default) =>
        await context.Set<SubscriptionPayment>().AddAsync(payment, ct);
}
