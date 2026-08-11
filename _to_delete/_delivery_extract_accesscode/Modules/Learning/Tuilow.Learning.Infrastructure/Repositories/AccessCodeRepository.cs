using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Learning.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.</summary>
public sealed class AccessCodeRepository(DbContext context) : IAccessCodeRepository
{
    public async Task<AccessCode?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<AccessCode>()
            .Include(a => a.Redemptions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<AccessCode>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<AccessCode>().ToListAsync(ct);

    public async Task AddAsync(AccessCode entity, CancellationToken ct = default) =>
        await context.Set<AccessCode>().AddAsync(entity, ct);

    public void Update(AccessCode entity) => context.Set<AccessCode>().Update(entity);
    public void Delete(AccessCode entity) => context.Set<AccessCode>().Remove(entity);

    public async Task<AccessCode?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await context.Set<AccessCode>()
            .Include(a => a.Redemptions)
            .FirstOrDefaultAsync(a => a.Code == code, ct);

    public async Task<IEnumerable<AccessCode>> GetAllAdminAsync(CancellationToken ct = default) =>
        await context.Set<AccessCode>()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Registra o AccessCodeRedemption explicitamente como Added no DbContext. Necessário porque
    /// DetectChanges marca entidades filhas com Guid novo como Modified (mesmo problema/fix de
    /// IEnrollmentRepository.AddLessonProgressAsync).
    /// </summary>
    public async Task AddRedemptionAsync(AccessCodeRedemption redemption, CancellationToken ct = default) =>
        await context.Set<AccessCodeRedemption>().AddAsync(redemption, ct);
}
