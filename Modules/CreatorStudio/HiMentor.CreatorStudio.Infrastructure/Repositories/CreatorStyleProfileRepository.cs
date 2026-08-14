using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.CreatorStudio.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class CreatorStyleProfileRepository(DbContext context) : ICreatorStyleProfileRepository
{
    public async Task<CreatorStyleProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CreatorStyleProfile>().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<CreatorStyleProfile>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CreatorStyleProfile>().ToListAsync(ct);

    public async Task AddAsync(CreatorStyleProfile entity, CancellationToken ct = default) =>
        await context.Set<CreatorStyleProfile>().AddAsync(entity, ct);

    public void Update(CreatorStyleProfile entity) => context.Set<CreatorStyleProfile>().Update(entity);
    public void Delete(CreatorStyleProfile entity) => context.Set<CreatorStyleProfile>().Remove(entity);

    public async Task<CreatorStyleProfile?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorStyleProfile>().FirstOrDefaultAsync(p => p.CreatorId == creatorId, ct);
}
