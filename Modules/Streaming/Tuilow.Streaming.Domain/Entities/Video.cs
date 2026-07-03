using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Streaming.Domain.Enums;

namespace Tuilow.Streaming.Domain.Entities;

public sealed class Video : AggregateRoot
{
    public string? CloudflareVideoId { get; private set; }
    public VideoStatus Status { get; private set; } = VideoStatus.Uploading;
    public int? DurationSeconds { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public long? SizeBytes { get; private set; }
    public bool IsProtected { get; private set; } = true;
    public DateTime? ReadyAt { get; private set; }

    // ─── Importação externa (passo 2 do assistente: "Conteúdo") ────────────────────
    public VideoSource Source { get; private set; } = VideoSource.Upload;
    /// <summary>URL original informada pelo criador (YouTube/Vimeo/Drive/Dropbox/OneDrive/CF Stream).</summary>
    public string? ExternalUrl { get; private set; }
    /// <summary>Id do vídeo na plataforma de origem (ex.: id do YouTube), quando aplicável.</summary>
    public string? ExternalId { get; private set; }
    public string? Title { get; private set; }

    private Video() { }

    public static Video Create() => new();

    /// <summary>
    /// Vídeo importado de uma plataforma externa via URL — não passa pelo Cloudflare Stream,
    /// então já nasce Ready (o conteúdo já está hospedado e pronto para reprodução em algum
    /// outro lugar; a plataforma só guarda a referência). Metadados (título/thumb/duração) vêm
    /// do IMediaImportService — quando a plataforma não expõe eles publicamente (Drive/Dropbox/
    /// OneDrive), ficam null e podem ser completados manualmente pelo criador.
    /// </summary>
    public static Video CreateFromExternal(
        VideoSource source, string externalUrl, string? externalId,
        string? title, int? durationSeconds, string? thumbnailUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUrl);

        var video = new Video
        {
            Source = source,
            ExternalUrl = externalUrl,
            ExternalId = externalId,
            Title = title,
            DurationSeconds = durationSeconds,
            ThumbnailUrl = thumbnailUrl,
            Status = VideoStatus.Ready,
            IsProtected = false, // conteúdo já público/hospedado fora da plataforma
            ReadyAt = DateTime.UtcNow
        };
        return video;
    }

    public void SetCloudflareVideoId(string videoId)
    {
        CloudflareVideoId = videoId;
        Status = VideoStatus.Processing;
        Touch();
    }

    public void MarkReady(int durationSeconds, string? thumbnailUrl = null)
    {
        Status = VideoStatus.Ready;
        DurationSeconds = durationSeconds;
        ThumbnailUrl = thumbnailUrl;
        ReadyAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkError()
    {
        Status = VideoStatus.Error;
        Touch();
    }
}
