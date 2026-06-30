namespace DogMaster.Application.Common.Interfaces;

public interface IStreamingService
{
    /// <summary>Gera URL de upload direto do browser para Cloudflare Stream.</summary>
    Task<string> GetDirectUploadUrlAsync(CancellationToken ct = default);

    /// <summary>Retorna signed URL de reprodução (JWT Cloudflare).</summary>
    Task<string> GetSignedPlaybackUrlAsync(string cloudflareVideoId, int expirationMinutes = 60, CancellationToken ct = default);

    /// <summary>Deleta vídeo do Cloudflare Stream.</summary>
    Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default);
}
