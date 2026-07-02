using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Streaming.Domain.Entities;

namespace Tuilow.Streaming.Domain.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default);
}
