using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Streaming.Entities;

namespace Tuilow.Domain.Contexts.Streaming.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default);
}
