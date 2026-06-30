using DogMaster.Domain.Contexts.DogProfile.Entities;
using DogMaster.Domain.Contexts.DogProfile.Interfaces;
using DogMaster.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DogMaster.Infrastructure.Repositories;

public sealed class DogRepository(ApplicationDbContext context) : IDogRepository
{
    public async Task<Dog?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Dogs.Include(d => d.Objectives).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IEnumerable<Dog>> GetAllAsync(CancellationToken ct = default) =>
        await context.Dogs.ToListAsync(ct);

    public async Task AddAsync(Dog entity, CancellationToken ct = default) =>
        await context.Dogs.AddAsync(entity, ct);

    public void Update(Dog entity) => context.Dogs.Update(entity);
    public void Delete(Dog entity) => context.Dogs.Remove(entity);

    public async Task<IEnumerable<Dog>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Dogs.Include(d => d.Objectives)
            .Where(d => d.UserId == userId).ToListAsync(ct);
}
