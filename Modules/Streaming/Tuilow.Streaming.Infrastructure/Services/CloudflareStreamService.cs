using System.Text;
using System.Text.Json;
using Tuilow.Streaming.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Tuilow.Streaming.Infrastructure.Services;

public sealed class CloudflareStreamService(
    HttpClient httpClient,
    IConfiguration configuration
) : IStreamingService
{
    private readonly string _accountId =
        !string.IsNullOrWhiteSpace(configuration["Cloudflare:AccountId"])
            ? configuration["Cloudflare:AccountId"]!
            : throw new InvalidOperationException(
                "Cloudflare:AccountId não configurado. Preencha appsettings.json > Cloudflare:AccountId.");

    public async Task<DirectUploadResult> GetDirectUploadUrlAsync(CancellationToken ct = default)
    {
        var body = JsonSerializer.Serialize(new
        {
            maxDurationSeconds = 3600,
            requireSignedURLs  = false  // URLs públicas — suficiente para dev/teste
        });

        var response = await httpClient.PostAsync(
            $"/client/v4/accounts/{_accountId}/stream/direct_upload",
            new StringContent(body, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Cloudflare Stream API retornou {(int)response.StatusCode}: {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc    = JsonDocument.Parse(content);
        var result = doc.RootElement.GetProperty("result");

        var uid       = result.GetProperty("uid").GetString()!;
        var uploadUrl = result.GetProperty("uploadURL").GetString()!;

        return new DirectUploadResult(uid, uploadUrl);
    }

    /// <summary>
    /// Retorna URL de playback.
    /// Em dev (requireSignedURLs=false) usa URL pública do iframe.
    /// Em produção, gere um par de chaves RS256 via API do Stream e implemente aqui.
    /// </summary>
    public Task<string> GetSignedPlaybackUrlAsync(
        string cloudflareVideoId,
        int expirationMinutes = 60,
        CancellationToken ct = default)
    {
        // URL pública — funciona enquanto requireSignedURLs = false
        return Task.FromResult(
            $"https://iframe.cloudflarestream.com/{cloudflareVideoId}");
    }

    public async Task DeleteVideoAsync(string cloudflareVideoId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync(
            $"/client/v4/accounts/{_accountId}/stream/{cloudflareVideoId}", ct);

        // 404 = vídeo já foi removido; ignora
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Upload direto (não-TUS) — usado pelo YouTubeDownloadWorker depois de baixar o vídeo com
    /// yt-dlp: o arquivo inteiro já está em disco, então um POST multipart simples é mais direto
    /// que simular um cliente TUS no servidor. Mesmo endpoint aceita tanto "uid" (upload de
    /// arquivo) quanto o direct_upload (TUS) usado no fluxo do navegador.
    /// </summary>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("video/mp4");
        content.Add(streamContent, "file", fileName);

        var response = await httpClient.PostAsync(
            $"/client/v4/accounts/{_accountId}/stream", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Cloudflare Stream API (upload) retornou {(int)response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("result").GetProperty("uid").GetString()!;
    }
}
