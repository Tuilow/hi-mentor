using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.CreatorStudio.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class LeadRepository(DbContext context) : ILeadRepository
{
    public async Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<Lead>().FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IEnumerable<Lead>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<Lead>().ToListAsync(ct);

    public async Task AddAsync(Lead entity, CancellationToken ct = default) =>
        await context.Set<Lead>().AddAsync(entity, ct);

    public void Update(Lead entity) => context.Set<Lead>().Update(entity);
    public void Delete(Lead entity) => context.Set<Lead>().Remove(entity);

    public async Task<IEnumerable<Lead>> ListByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Set<Lead>()
            .Where(l => l.CourseId == courseId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Set<Lead>().CountAsync(l => l.CourseId == courseId, ct);
}
