using Tuilow.CreatorStudio.Domain.Entities;
using Tuilow.CreatorStudio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.CreatorStudio.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class LessonScriptRepository(DbContext context) : ILessonScriptRepository
{
    public async Task<LessonScript?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<LessonScript>().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IEnumerable<LessonScript>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<LessonScript>().ToListAsync(ct);

    public async Task AddAsync(LessonScript entity, CancellationToken ct = default) =>
        await context.Set<LessonScript>().AddAsync(entity, ct);

    public void Update(LessonScript entity) => context.Set<LessonScript>().Update(entity);
    public void Delete(LessonScript entity) => context.Set<LessonScript>().Remove(entity);

    public async Task<IEnumerable<LessonScript>> ListByCreatorAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<LessonScript>()
            .Where(s => s.CreatorId == creatorId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<int> CountRecordedByCreatorAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<LessonScript>().CountAsync(s => s.CreatorId == creatorId && s.WasRecorded, ct);
}
