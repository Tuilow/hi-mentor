using System.Threading.Channels;
using Tuilow.Streaming.Application.Interfaces;

namespace Tuilow.Streaming.Infrastructure.BackgroundJobs;

public sealed record YouTubeDownloadJob(Guid VideoId, string SourceUrl);

/// <summary>
/// Fila em memória (Channel, não durável) de downloads pendentes. Enqueue é chamado pelo
/// ImportExternalVideoCommandHandler (via IYouTubeDownloadQueue); o YouTubeDownloadWorker
/// consome via Reader. Se a aplicação reiniciar com itens ainda na fila, eles se perdem — risco
/// aceito explicitamente (ver plano): o vídeo fica parado em Status=Uploading e o criador
/// reimporta a URL manualmente. Registrado como singleton (precisa ser a MESMA instância entre
/// quem escreve — handler, escopo por requisição — e quem lê — o worker, singleton).
/// </summary>
public sealed class YouTubeDownloadQueue : IYouTubeDownloadQueue
{
    private readonly Channel<YouTubeDownloadJob> _channel = Channel.CreateUnbounded<YouTubeDownloadJob>();

    public ChannelReader<YouTubeDownloadJob> Reader => _channel.Reader;

    public void Enqueue(Guid videoId, string sourceUrl) =>
        _channel.Writer.TryWrite(new YouTubeDownloadJob(videoId, sourceUrl));
}
