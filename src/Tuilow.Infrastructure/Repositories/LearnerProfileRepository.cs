using Tuilow.Domain.Contexts.Profiles.Entities;
using Tuilow.Domain.Contexts.Profiles.Interfaces;
using Tuilow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Infrastructure.Repositories;

public sealed class LearnerProfileRepository(ApplicationDbContext context) : ILearnerProfileRepository
{
    public async Task<LearnerProfile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.LearnerProfiles.Include(p => p.Goals).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<LearnerProfile>> GetAllAsync(CancellationToken ct = default) =>
        await context.LearnerProfiles.ToListAsync(ct);

    public async Task AddAsync(LearnerProfile entity, CancellationToken ct = default) =>
        await context.LearnerProfiles.AddAsync(entity, ct);

    public void Update(LearnerProfile entity) => context.LearnerProfiles.Update(entity);
    public void Delete(LearnerProfile entity) => context.LearnerProfiles.Remove(entity);

    public async Task<IEnumerable<LearnerProfile>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.LearnerProfiles.Include(p => p.Goals)
            .Where(p => p.UserId == userId).ToListAsync(ct);
}
