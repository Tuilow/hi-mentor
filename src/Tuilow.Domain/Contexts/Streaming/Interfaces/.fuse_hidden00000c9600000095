using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Streaming.Entities;

namespace DogMaster.Domain.Contexts.Streaming.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default);
}
