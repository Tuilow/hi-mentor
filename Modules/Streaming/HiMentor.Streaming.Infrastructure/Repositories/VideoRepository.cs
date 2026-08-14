using HiMentor.Streaming.Domain.Entities;
using HiMentor.Streaming.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Streaming.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class VideoRepository(DbContext context) : IVideoRepository
{
    public async Task<Video?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<Video>().FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IEnumerable<Video>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<Video>().ToListAsync(ct);

    public async Task AddAsync(Video entity, CancellationToken ct = default) =>
        await context.Set<Video>().AddAsync(entity, ct);

    public void Update(Video entity) => context.Set<Video>().Update(entity);
    public void Delete(Video entity) => context.Set<Video>().Remove(entity);

    public async Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default) =>
        await context.Set<Video>().FirstOrDefaultAsync(v => v.CloudflareVideoId == cloudflareVideoId, ct);

    public async Task<IEnumerable<Video>> ListByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Set<Video>()
            .Where(v => v.CourseId == courseId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);
}
