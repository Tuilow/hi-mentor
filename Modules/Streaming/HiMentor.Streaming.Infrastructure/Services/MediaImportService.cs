using System.Text.Json;
using System.Text.RegularExpressions;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.Streaming.Application.Interfaces;
using HiMentor.Streaming.Domain.Enums;

namespace HiMentor.Streaming.Infrastructure.Services;

/// <summary>
/// Implementação real (sem necessidade de API key) do passo 2 do assistente — importar vídeo
/// por URL em vez de subir o arquivo.
///
/// YouTube e Vimeo: usa os endpoints públicos de oEmbed de cada plataforma — retornam título e
/// thumbnail de verdade (Vimeo também retorna duração) sem autenticação. Google Drive, Dropbox,
/// OneDrive e Cloudflare Stream não têm oEmbed público sem OAuth — para esses, o serviço apenas
/// reconhece a plataforma pela URL e guarda a referência (best-effort); título/duração ficam
/// null e podem ser preenchidos manualmente pelo criador no wizard.
/// </summary>
public sealed partial class MediaImportService(HttpClient httpClient) : IMediaImportService
{
    public async Task<ImportedMediaMetadata> FetchMetadataAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            throw new BusinessException("URL inválida.");

        var host = uri.Host.ToLowerInvariant();

        if (IsHost(host, "youtube.com") || IsHost(host, "youtu.be"))
            return await FetchYouTubeAsync(url, ct);

        if (IsHost(host, "vimeo.com"))
            return await FetchVimeoAsync(url, ct);

        if (IsHost(host, "drive.google.com"))
            return BestEffort(VideoSource.GoogleDrive, url, ExtractGoogleDriveFileId(url));

        if (IsHost(host, "dropbox.com"))
            return BestEffort(VideoSource.Dropbox, url, null);

        if (IsHost(host, "onedrive.live.com") || IsHost(host, "1drv.ms") || IsHost(host, "sharepoint.com"))
            return BestEffort(VideoSource.OneDrive, url, null);

        if (IsHost(host, "cloudflarestream.com") || IsHost(host, "videodelivery.net"))
            return BestEffort(VideoSource.CloudflareStream, url, ExtractCloudflareStreamId(url));

        throw new BusinessException(
            "Plataforma não suportada para importação. Use YouTube, Vimeo, Cloudflare Stream, Google Drive, Dropbox ou OneDrive.");
    }

    /// <summary>
    /// Compara o host de forma ANCORADA (igual ao domínio ou subdomínio dele, com ponto antes)
    /// — nunca por substring solta. "host.Contains("youtube.com")" combinaria também com um
    /// domínio forjado tipo "youtube.com.attacker.io", classificando (e servindo aos alunos)
    /// uma URL de phishing como se fosse YouTube de verdade.
    /// </summary>
    private static bool IsHost(string host, string domain) =>
        host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);

    private async Task<ImportedMediaMetadata> FetchYouTubeAsync(string url, CancellationToken ct)
    {
        var videoId = ExtractYouTubeId(url);
        var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json";

        try
        {
            using var response = await httpClient.GetAsync(oembedUrl, ct);
            if (!response.IsSuccessStatusCode)
                return BestEffort(VideoSource.YouTube, url, videoId);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var thumbnail = root.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : null;

            // oEmbed do YouTube não retorna duração (exigiria a Data API v3 com chave própria) —
            // o criador pode ajustar manualmente no wizard se necessário.
            return new ImportedMediaMetadata(VideoSource.YouTube, url, videoId, title, thumbnail, null);
        }
        catch (Exception)
        {
            // Falha de rede/oEmbed não deve travar a importação — guarda a URL mesmo assim.
            return BestEffort(VideoSource.YouTube, url, videoId);
        }
    }

    private async Task<ImportedMediaMetadata> FetchVimeoAsync(string url, CancellationToken ct)
    {
        var videoId = ExtractVimeoId(url);
        var oembedUrl = $"https://vimeo.com/api/oembed.json?url={Uri.EscapeDataString(url)}";

        try
        {
            using var response = await httpClient.GetAsync(oembedUrl, ct);
            if (!response.IsSuccessStatusCode)
                return BestEffort(VideoSource.Vimeo, url, videoId);

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = root.TryGetProperty("title", out var t) ? t.GetString() : null;
            var thumbnail = root.TryGetProperty("thumbnail_url", out var th) ? th.GetString() : null;
            int? duration = root.TryGetProperty("duration", out var d) && d.TryGetInt32(out var dv) ? dv : null;

            return new ImportedMediaMetadata(VideoSource.Vimeo, url, videoId, title, thumbnail, duration);
        }
        catch (Exception)
        {
            return BestEffort(VideoSource.Vimeo, url, videoId);
        }
    }

    private static ImportedMediaMetadata BestEffort(VideoSource source, string url, string? externalId) =>
        new(source, url, externalId, null, null, null);

    private static string? ExtractYouTubeId(string url)
    {
        var match = YouTubeIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractVimeoId(string url)
    {
        var match = VimeoIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractGoogleDriveFileId(string url)
    {
        var match = GoogleDriveIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractCloudflareStreamId(string url)
    {
        var match = CloudflareStreamIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?:v=|youtu\.be/|embed/)([a-zA-Z0-9_-]{6,})")]
    private static partial Regex YouTubeIdRegex();

    [GeneratedRegex(@"vimeo\.com/(?:.*/)?(\d+)")]
    private static partial Regex VimeoIdRegex();

    [GeneratedRegex(@"/d/([a-zA-Z0-9_-]+)")]
    private static partial Regex GoogleDriveIdRegex();

    [GeneratedRegex(@"(?:videodelivery\.net/|cloudflarestream\.com/)([a-zA-Z0-9]+)")]
    private static partial Regex CloudflareStreamIdRegex();
}
