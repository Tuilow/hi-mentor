using DogMaster.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace DogMaster.Infrastructure.Services.Streaming;

/// <summary>
/// Implementação mock do IStreamingService para desenvolvimento sem Cloudflare Stream ativo.
/// Ative com: "Cloudflare": { "MockMode": true } no appsettings.
///
/// Fluxo mock:
///   1. GetDirectUploadUrlAsync → uid falso + uploadUrl apontando para /api/v1/mock/tus/{uid}
///   2. Frontend faz upload TUS para o endpoint mock (aceita qualquer arquivo)
///   3. Endpoint mock marca o vídeo como pronto no banco automaticamente
///   4. GetSignedPlaybackUrlAsync → URL de vídeo de demonstração público (Big Buck Bunny)
/// </summary>
public sealed class MockStreamingService(IConfiguration configuration) : IStreamingService
{
    // Vídeo de demonstração público para preview durante dev
    private const string SampleVideoUrl =
        "https://iframe.cloudflarestream.com/31c9291ab41fac05471db4e73aa11717";

    private string ApiBaseUrl =>
        configuration["FrontendUrl"] is { } url
            ? url.Replace("3000", "57881") // troca porta do frontend para a da API
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
        // Em mock, retorna sempre o mesmo vídeo de demonstração
        return Task.FromResult(SampleVideoUrl);
    }

    public Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default)
        => Task.CompletedTask; // No-op em mock
}
