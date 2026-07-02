using Tuilow.Streaming.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Tuilow.Streaming.Infrastructure.Services;

/// <summary>
/// Implementação mock do IStreamingService para desenvolvimento sem Cloudflare Stream ativo.
/// Fluxo mock:
///   1. GetDirectUploadUrlAsync  → uid falso + uploadUrl → /api/v1/mock/tus/{uid}
///   2. MockTusController salva o vídeo em disco em mock-videos/{uid}
///   3. MockTusController marca o vídeo como pronto no banco
///   4. GetSignedPlaybackUrlAsync → /api/v1/mock/videos/{uid}  (serve o arquivo local)
/// </summary>
public sealed class MockStreamingService(IConfiguration configuration) : IStreamingService
{
    private string ApiBaseUrl =>
        configuration["FrontendUrl"] is { } url
            ? url.Replace("3000", "57881")
            : "http://localhost:57881";

    public Task<DirectUploadResult> GetDirectUploadUrlAsync(CancellationToken ct = default)
    {
        var fakeUid   = Guid.NewGuid().ToString("N");
        var uploadUrl = $"{ApiBaseUrl}/api/v1/mock/tus/{fakeUid}";
        return Task.FromResult(new DirectUploadResult(fakeUid, uploadUrl));
    }

    public Task<string> GetSignedPlaybackUrlAsync(
        string cloudflareVideoId,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        // Retorna a URL do arquivo salvo em disco pelo MockTusController
        return Task.FromResult($"{ApiBaseUrl}/api/v1/mock/videos/{cloudflareVideoId}");
    }

    public Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default)
        => Task.CompletedTask;
}
