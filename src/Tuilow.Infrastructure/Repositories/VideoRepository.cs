using Tuilow.Domain.Contexts.Streaming.Entities;
using Tuilow.Domain.Contexts.Streaming.Interfaces;
using Tuilow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Infrastructure.Repositories;

public sealed class VideoRepository(ApplicationDbContext context) : IVideoRepository
{
    public async Task<Video?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Videos.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IEnumerable<Video>> GetAllAsync(CancellationToken ct = default) =>
        await context.Videos.ToListAsync(ct);

    public async Task AddAsync(Video entity, CancellationToken ct = default) =>
        await context.Videos.AddAsync(entity, ct);

    public void Update(Video entity) => context.Videos.Update(entity);
    public void Delete(Video entity) => context.Videos.Remove(entity);

    public async Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default) =>
        await context.Videos.FirstOrDefaultAsync(v => v.CloudflareVideoId == cloudflareVideoId, ct);
}
