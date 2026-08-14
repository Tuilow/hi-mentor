using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Finance.Infrastructure.Repositories;

public sealed class PlatformFeeConfigurationRepository(DbContext context) : IPlatformFeeConfigurationRepository
{
    public async Task<PlatformFeeConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<PlatformFeeConfiguration>().FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IEnumerable<PlatformFeeConfiguration>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<PlatformFeeConfiguration>().ToListAsync(ct);

    public async Task AddAsync(PlatformFeeConfiguration entity, CancellationToken ct = default) =>
        await context.Set<PlatformFeeConfiguration>().AddAsync(entity, ct);

    public void Update(PlatformFeeConfiguration entity) => context.Set<PlatformFeeConfiguration>().Update(entity);
    public void Delete(PlatformFeeConfiguration entity) => context.Set<PlatformFeeConfiguration>().Remove(entity);

    public async Task<PlatformFeeConfiguration?> GetActiveAsync(CancellationToken ct = default) =>
        await context.Set<PlatformFeeConfiguration>()
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    public async Task<IEnumerable<PlatformFeeConfiguration>> GetHistoryAsync(CancellationToken ct = default) =>
        await context.Set<PlatformFeeConfiguration>()
            .OrderByDescending(f => f.EffectiveFrom)
            .ToListAsync(ct);
}
