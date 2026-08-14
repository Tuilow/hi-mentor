using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using CoursePurchaseEntity = HiMentor.Sales.Domain.Entities.CoursePurchase;

namespace HiMentor.Sales.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.</summary>
public sealed class CoursePurchaseRepository(DbContext context) : ICoursePurchaseRepository
{
    public async Task<CoursePurchaseEntity?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<CoursePurchaseEntity>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>().ToListAsync(ct);

    public async Task AddAsync(CoursePurchaseEntity entity, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>().AddAsync(entity, ct);

    public void Update(CoursePurchaseEntity entity) => context.Set<CoursePurchaseEntity>().Update(entity);
    public void Delete(CoursePurchaseEntity entity) => context.Set<CoursePurchaseEntity>().Remove(entity);

    public async Task<CoursePurchaseEntity?> GetByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>().FirstOrDefaultAsync(p => p.AsaasPaymentId == asaasPaymentId, ct);

    public async Task<bool> HasConfirmedPurchaseAsync(Guid studentId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>().AnyAsync(p =>
            p.StudentId == studentId && p.CourseId == courseId && p.Status == CoursePurchaseStatus.Confirmed, ct);

    public async Task<IEnumerable<CoursePurchaseEntity>> GetByStudentAsync(Guid studentId, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>()
            .Where(p => p.StudentId == studentId)
            .ToListAsync(ct);

    public async Task<IEnumerable<CoursePurchaseEntity>> GetByCreatorAsync(
        Guid creatorId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = context.Set<CoursePurchaseEntity>().Where(p => p.CreatorId == creatorId);
        if (from is not null) query = query.Where(p => p.CreatedAt >= from);
        if (to is not null) query = query.Where(p => p.CreatedAt <= to);
        return await query.ToListAsync(ct);
    }

    // B4: usado pelo job periódico que expira compras Pending abandonadas (checkout nunca concluído).
    public async Task<IEnumerable<CoursePurchaseEntity>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>()
            .Where(p => p.Status == CoursePurchaseStatus.Pending && p.CreatedAt < threshold)
            .ToListAsync(ct);

    // A5: usado pelo job de reconciliação (Confirmed sem crédito correspondente na carteira do
    // criador). Restrito a PaymentModel == Legacy: uma venda MarketplaceSplit NUNCA gera
    // WalletTransaction (o dinheiro é liquidado direto pelo split da Asaas, sem passar pela
    // conta da HiMentor) — sem este filtro, toda venda marketplace confirmada seria sinalizada
    // como uma falha crítica por engano.
    public async Task<IEnumerable<CoursePurchaseEntity>> GetConfirmedForReconciliationAsync(
        DateTime lookbackFloor, DateTime graceThreshold, CancellationToken ct = default) =>
        await context.Set<CoursePurchaseEntity>()
            .Where(p => p.Status == CoursePurchaseStatus.Confirmed
                && p.PaymentModel == CoursePurchasePaymentModel.Legacy
                && p.ConfirmedAt != null
                && p.ConfirmedAt >= lookbackFloor
                && p.ConfirmedAt <= graceThreshold)
            .ToListAsync(ct);

    public async Task<(decimal GrossTotal, decimal CommissionTotal, decimal CreatorNetTotal, int SalesCount)> GetMarketplaceTotalsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = context.Set<CoursePurchaseEntity>()
            .Where(p => p.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit && p.Status == CoursePurchaseStatus.Confirmed);

        if (from is not null) query = query.Where(p => p.ConfirmedAt >= from);
        if (to is not null) query = query.Where(p => p.ConfirmedAt <= to);

        var rows = await query
            .Select(p => new { Gross = p.Amount.Amount, Commission = p.PlatformCommissionAmount!.Amount, Net = p.CreatorNetAmount!.Amount })
            .ToListAsync(ct);

        return (rows.Sum(r => r.Gross), rows.Sum(r => r.Commission), rows.Sum(r => r.Net), rows.Count);
    }
}
