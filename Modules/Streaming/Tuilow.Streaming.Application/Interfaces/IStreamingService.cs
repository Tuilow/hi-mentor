namespace Tuilow.Streaming.Application.Interfaces;

/// <summary>Resultado do direct upload do Cloudflare Stream.</summary>
public sealed record DirectUploadResult(
    string CloudflareVideoId, // uid gerado pelo Cloudflare (ex: "ea95132c15732418...")
    string UploadUrl          // endpoint TUS para o browser fazer upload direto
);

public interface IStreamingService
{
    /// <summary>
    /// Cria um slot de upload no Cloudflare Stream.
    /// Retorna o uid do vídeo (para salvar no DB) e a UploadUrl TUS (para o browser usar).
    /// </summary>
    Task<DirectUploadResult> GetDirectUploadUrlAsync(CancellationToken ct = default);

    /// <summary>
    /// Gera URL de playback assinada com JWT (expira em expirationMinutes).
    /// Se StreamSigningKey não estiver configurado, retorna URL pública (modo dev).
    /// </summary>
    Task<string> GetSignedPlaybackUrlAsync(
        string cloudflareVideoId,
        int expirationMinutes = 60,
        CancellationToken ct = default);

    /// <summary>Remove o vídeo do Cloudflare Stream.</summary>
    Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default);
}
