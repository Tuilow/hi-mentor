using HiMentor.SharedKernel.Domain.Common;
using HiMentor.Streaming.Domain.Enums;

namespace HiMentor.Streaming.Domain.Entities;

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

    /// <summary>
    /// Produto ao qual este vídeo pertence (passo 2 do assistente é sempre executado com o
    /// produto já criado no passo 1). Permite recarregar "meus vídeos deste produto ainda não
    /// vinculados a uma aula" ao reabrir o assistente — sem isso, um vídeo enviado/importado
    /// ficava só na memória da página e "sumia" se o criador saísse e voltasse ao assistente
    /// antes de vinculá-lo a uma aula no passo 3.
    /// </summary>
    public Guid? CourseId { get; private set; }

    private Video() { }

    /// <summary>
    /// Upload direto (passo 2 do assistente, arquivo enviado pelo próprio criador). Achado
    /// (12/08/2026): antes não recebia título nenhum — Title ficava sempre null e a lista
    /// "Vídeos disponíveis para vincular" caía no fallback "(sem título)" assim que a página
    /// recarregava os vídeos do servidor (o nome do arquivo só existia otimisticamente no
    /// estado local do React, nunca era persistido). Title opcional para não quebrar nenhum
    /// outro chamador existente de Create.
    /// </summary>
    public static Video Create(Guid? courseId = null, string? title = null) =>
        new() { CourseId = courseId, Title = title };

    /// <summary>
    /// Vídeo importado de uma plataforma externa via URL — não passa pelo Cloudflare Stream,
    /// então já nasce Ready (o conteúdo já está hospedado e pronto para reprodução em algum
    /// outro lugar; a plataforma só guarda a referência). Metadados (título/thumb/duração) vêm
    /// do IMediaImportService — quando a plataforma não expõe eles publicamente (Drive/Dropbox/
    /// OneDrive), ficam null e podem ser completados manualmente pelo criador.
    /// </summary>
    public static Video CreateFromExternal(
        VideoSource source, string externalUrl, string? externalId,
        string? title, int? durationSeconds, string? thumbnailUrl, Guid? courseId = null)
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
            ReadyAt = DateTime.UtcNow,
            CourseId = courseId
        };
        return video;
    }

    /// <summary>
    /// Vídeo do YouTube que o criador pediu para BAIXAR e hospedar na plataforma (checkbox no
    /// passo 2 do assistente), em vez de só referenciar o link (CreateFromExternal). Diferente
    /// de CreateFromExternal: nasce Uploading (não Ready) e IsProtected=true — o
    /// YouTubeDownloadWorker baixa o arquivo e chama SetCloudflareVideoId, e daí em diante o
    /// vídeo segue o mesmo ciclo de vida de um upload comum (Processing → Ready via webhook do
    /// Cloudflare). ExternalUrl/ExternalId/Title continuam preenchidos com a URL original, só
    /// como referência/auditoria de onde o conteúdo veio.
    /// </summary>
    public static Video CreateDownloading(
        VideoSource source, string externalUrl, string? externalId, string? title, Guid? courseId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalUrl);

        return new Video
        {
            Source = source,
            ExternalUrl = externalUrl,
            ExternalId = externalId,
            Title = title,
            Status = VideoStatus.Uploading,
            IsProtected = true, // vai virar conteúdo hospedado (e protegido) no Cloudflare Stream
            CourseId = courseId
        };
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
