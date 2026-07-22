using Tuilow.Streaming.Application.Interfaces;
using Microsoft.AspNetCore.Http;
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
public sealed class MockStreamingService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IStreamingService
{
    /// <summary>
    /// Monta a base URL a partir da requisição HTTP atual (scheme+host+porta que o browser
    /// realmente usou para chamar a API) — funciona em qualquer cenário (dotnet run com porta
    /// aleatória do launchSettings.json, Docker, IIS, etc.), diferente de tentar adivinhar a
    /// porta da API a partir da FrontendUrl (bug antigo: só funcionava se a API estivesse,
    /// por coincidência, na porta 57881).
    /// </summary>
    private string ApiBaseUrl
    {
        get
        {
            var request = httpContextAccessor.HttpContext?.Request;
            if (request is not null)
                return $"{request.Scheme}://{request.Host}";

            // Fallback apenas para chamadas fora de um contexto HTTP (ex.: testes/jobs em background).
            return configuration["Cloudflare:MockBaseUrl"] ?? "http://localhost:5000";
        }
    }

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

    /// <summary>
    /// Mock do upload direto usado pelo YouTubeDownloadWorker: salva o arquivo já baixado em
    /// mock-videos/{uid} (mesma pasta/convenção do MockTusController), servido pelo mesmo GET
    /// /api/v1/mock/videos/{uid}. Diferente do fluxo TUS, aqui não há webhook do Cloudflare pra
    /// marcar o vídeo como pronto — em modo mock o vídeo fica em "Processing" (arquivo já
    /// acessível via GetSignedPlaybackUrlAsync, mas o status na UI não avança sozinho). Isso é
    /// aceitável: MockMode é só para desenvolvimento local sem credenciais reais do Cloudflare.
    /// </summary>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var fakeUid = Guid.NewGuid().ToString("N");
        var mockVideosDir = Path.Combine(Directory.GetCurrentDirectory(), "mock-videos");
        Directory.CreateDirectory(mockVideosDir);

        await using var fs = new FileStream(Path.Combine(mockVideosDir, fakeUid), FileMode.Create, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(fs, ct);

        return fakeUid;
    }
}
