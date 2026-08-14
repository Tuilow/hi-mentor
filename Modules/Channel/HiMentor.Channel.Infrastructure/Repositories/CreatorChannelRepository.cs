using Microsoft.EntityFrameworkCore;
using HiMentor.Channel.Domain.Entities;
using HiMentor.Channel.Domain.Interfaces;

namespace HiMentor.Channel.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class CreatorChannelRepository(DbContext context) : ICreatorChannelRepository
{
    public async Task<CreatorChannel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CreatorChannel>().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<CreatorChannel>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CreatorChannel>().ToListAsync(ct);

    public async Task AddAsync(CreatorChannel entity, CancellationToken ct = default) =>
        await context.Set<CreatorChannel>().AddAsync(entity, ct);

    public void Update(CreatorChannel entity) => context.Set<CreatorChannel>().Update(entity);
    public void Delete(CreatorChannel entity) => context.Set<CreatorChannel>().Remove(entity);

    public async Task<CreatorChannel?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorChannel>().FirstOrDefaultAsync(c => c.CreatorId == creatorId, ct);

    public async Task<CreatorChannel?> GetByHandleAsync(string handle, CancellationToken ct = default)
    {
        var normalized = handle.Trim().TrimStart('@').ToLowerInvariant();
        return await context.Set<CreatorChannel>()
            .FirstOrDefaultAsync(c => c.Handle == normalized, ct);
    }

    public async Task<bool> HandleExistsAsync(string handle, Guid? excludeChannelId = null, CancellationToken ct = default)
    {
        var normalized = handle.Trim().TrimStart('@').ToLowerInvariant();
        var query = context.Set<CreatorChannel>().Where(c => c.Handle == normalized);
        if (excludeChannelId.HasValue)
            query = query.Where(c => c.Id != excludeChannelId.Value);
        return await query.AnyAsync(ct);
    }
}
