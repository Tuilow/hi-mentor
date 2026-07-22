using System.Diagnostics;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tuilow.Streaming.Infrastructure.BackgroundJobs;

/// <summary>
/// Consome a YouTubeDownloadQueue um item por vez: baixa o vídeo com yt-dlp (subprocesso) para
/// um arquivo temporário, sobe pro Cloudflare Stream (IStreamingService.UploadFileAsync) e
/// associa o uid ao Video (SetCloudflareVideoId, que já deixa Status=Processing) — o resto do
/// pipeline (transcodificação, thumbnail, duração, "pronto") já é automático via
/// CloudflareStreamWebhookController, exatamente como em um upload comum via TUS. Roda no mesmo
/// processo do Host.Api (sem infraestrutura de fila nova, ver YouTubeDownloadQueue).
/// </summary>
public sealed class YouTubeDownloadWorker(
    YouTubeDownloadQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<YouTubeDownloadWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(job, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao processar download do YouTube para o vídeo {VideoId}.", job.VideoId);
                await MarkErrorAsync(job.VideoId, stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(YouTubeDownloadJob job, CancellationToken ct)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"yt-{job.VideoId:N}.mp4");
        try
        {
            logger.LogInformation("Baixando vídeo do YouTube para o Video {VideoId}: {Url}", job.VideoId, job.SourceUrl);
            await DownloadWithYtDlpAsync(job.SourceUrl, tempFile, ct);

            using var scope = scopeFactory.CreateScope();
            var streamingService = scope.ServiceProvider.GetRequiredService<IStreamingService>();
            var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            string cloudflareVideoId;
            await using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                cloudflareVideoId = await streamingService.UploadFileAsync(fileStream, Path.GetFileName(tempFile), ct);
            }

            var video = await videoRepository.GetByIdAsync(job.VideoId, ct);
            if (video is null)
            {
                logger.LogWarning("Video {VideoId} não encontrado ao concluir o download — descartando.", job.VideoId);
                return;
            }

            video.SetCloudflareVideoId(cloudflareVideoId);
            videoRepository.Update(video);
            await uow.SaveChangesAsync(ct);

            logger.LogInformation(
                "Vídeo {VideoId} baixado do YouTube e enviado ao Cloudflare Stream (uid={Uid}).",
                job.VideoId, cloudflareVideoId);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                try { File.Delete(tempFile); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Não foi possível apagar o arquivo temporário {TempFile}.", tempFile);
                }
            }
        }
    }

    /// <summary>
    /// Roda o binário standalone do yt-dlp (instalado no Dockerfile) como subprocesso. Formato
    /// escolhido prioriza mp4 pronto (evita remux quando possível) e cai para o melhor vídeo+
    /// áudio separados, juntando com ffmpeg (também instalado no Dockerfile) via
    /// --merge-output-format. --max-filesize é uma rede de segurança contra vídeos absurdamente
    /// grandes consumindo todo o disco do container.
    /// </summary>
    private static async Task DownloadWithYtDlpAsync(string url, string outputPath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(url);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/b");
        psi.ArgumentList.Add("--merge-output-format");
        psi.ArgumentList.Add("mp4");
        psi.ArgumentList.Add("--no-playlist");
        psi.ArgumentList.Add("--max-filesize");
        psi.ArgumentList.Add("2G");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var stderr = await stderrTask;
            throw new InvalidOperationException($"yt-dlp falhou (exit code {process.ExitCode}): {stderr}");
        }

        await stdoutTask;

        if (!File.Exists(outputPath))
            throw new InvalidOperationException("yt-dlp terminou sem erro, mas o arquivo de saída não foi encontrado.");
    }

    private async Task MarkErrorAsync(Guid videoId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var video = await videoRepository.GetByIdAsync(videoId, ct);
            if (video is null) return;

            video.MarkError();
            videoRepository.Update(video);
            await uow.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao marcar o vídeo {VideoId} como erro.", videoId);
        }
    }
}
