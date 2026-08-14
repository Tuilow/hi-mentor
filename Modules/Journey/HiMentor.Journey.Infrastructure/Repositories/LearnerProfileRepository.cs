using HiMentor.Journey.Domain.Entities;
using HiMentor.Journey.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Journey.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class LearnerProfileRepository(DbContext context) : ILearnerProfileRepository
{
    public async Task<LearnerProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<LearnerProfile>().Include(p => p.Goals).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<LearnerProfile>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<LearnerProfile>().ToListAsync(ct);

    public async Task AddAsync(LearnerProfile entity, CancellationToken ct = default) =>
        await context.Set<LearnerProfile>().AddAsync(entity, ct);

    public void Update(LearnerProfile entity) => context.Set<LearnerProfile>().Update(entity);
    public void Delete(LearnerProfile entity) => context.Set<LearnerProfile>().Remove(entity);

    public async Task<IEnumerable<LearnerProfile>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Set<LearnerProfile>().Include(p => p.Goals)
            .Where(p => p.UserId == userId).ToListAsync(ct);
}
