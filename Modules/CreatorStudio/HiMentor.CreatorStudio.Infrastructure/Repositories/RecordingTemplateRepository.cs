using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.CreatorStudio.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class RecordingTemplateRepository(DbContext context) : IRecordingTemplateRepository
{
    public async Task<RecordingTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<RecordingTemplate>().FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IEnumerable<RecordingTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<RecordingTemplate>().ToListAsync(ct);

    public async Task AddAsync(RecordingTemplate entity, CancellationToken ct = default) =>
        await context.Set<RecordingTemplate>().AddAsync(entity, ct);

    public void Update(RecordingTemplate entity) => context.Set<RecordingTemplate>().Update(entity);
    public void Delete(RecordingTemplate entity) => context.Set<RecordingTemplate>().Remove(entity);

    public async Task<IEnumerable<RecordingTemplate>> ListByCreatorAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<RecordingTemplate>()
            .Where(t => t.CreatorId == creatorId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<RecordingTemplate?> GetDefaultByCreatorAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<RecordingTemplate>()
            .FirstOrDefaultAsync(t => t.CreatorId == creatorId && t.IsDefault, ct);
}
