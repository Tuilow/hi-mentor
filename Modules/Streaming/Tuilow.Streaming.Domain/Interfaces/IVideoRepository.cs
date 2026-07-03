using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Streaming.Domain.Entities;

namespace Tuilow.Streaming.Domain.Interfaces;

public interface IVideoRepository : IRepository<Video>
{
    Task<Video?> GetByCloudflareIdAsync(string cloudflareVideoId, CancellationToken ct = default);

    /// <summary>Vídeos já enviados/importados para este produto — usado para reidratar o passo 2 do assistente.</summary>
    Task<IEnumerable<Video>> ListByCourseAsync(Guid courseId, CancellationToken ct = default);
}
