using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using CoursePurchaseEntity = Tuilow.Sales.Domain.Entities.CoursePurchase;

namespace Tuilow.Sales.Infrastructure.Repositories;

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
}
