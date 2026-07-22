namespace Tuilow.Streaming.Application.Interfaces;

/// <summary>
/// Fila (em memória, não durável — ver YouTubeDownloadQueue na Infrastructure) de downloads de
/// vídeo pendentes. ImportExternalVideoCommandHandler chama Enqueue depois de criar o Video em
/// Status=Uploading; o YouTubeDownloadWorker consome e processa em segundo plano.
/// </summary>
public interface IYouTubeDownloadQueue
{
    void Enqueue(Guid videoId, string sourceUrl);
}
