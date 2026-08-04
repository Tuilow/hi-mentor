using System.Diagnostics;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
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
///
/// Exceção: em Cloudflare:MockMode (dev local sem conta real do Cloudflare), não existe nenhum
/// webhook de verdade chegando — MockStreamingService só salva o arquivo em mock-videos/{uid} e
/// não há nada do lado de fora avisando "terminei de processar". Por isso, em mock mode, este
/// worker já marca o vídeo como pronto na hora (mesma solução que o MockTusController usa para o
/// fluxo de upload via TUS no navegador).
/// </summary>
public sealed class YouTubeDownloadWorker(
    YouTubeDownloadQueue queue,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
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
        string? cookiesFilePath = null;
        try
        {
            // Achado de teste manual: o YouTube bloqueia downloads vindos de IP de datacenter
            // (Railway, AWS etc.) com "Sign in to confirm you're not a bot", mesmo pra vídeos
            // públicos comuns -- não dá pra contornar só ajustando flags do yt-dlp. Passar os
            // cookies de uma sessão logada de verdade (exportados como cookies.txt formato
            // Netscape, configurados em YtDlp:CookiesContent) convence o YouTube de que a
            // requisição vem de um navegador autenticado. Precisam ser renovados periodicamente
            // (expiram) -- sem cookies configurados, cai no comportamento antigo (sem --cookies).
            var cookiesContent = configuration["YtDlp:CookiesContent"];
            if (!string.IsNullOrWhiteSpace(cookiesContent))
            {
                cookiesFilePath = Path.Combine(Path.GetTempPath(), $"yt-cookies-{job.VideoId:N}.txt");
                await File.WriteAllTextAsync(cookiesFilePath, cookiesContent, ct);
            }

            logger.LogInformation("Baixando vídeo do YouTube para o Video {VideoId}: {Url}", job.VideoId, job.SourceUrl);
            await DownloadWithYtDlpAsync(job.SourceUrl, tempFile, cookiesFilePath, ct);

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

            if (configuration.GetValue<bool>("Cloudflare:MockMode"))
            {
                // Sem Cloudflare real, sem webhook — marca pronto direto (ver comentário na
                // classe). Duração fixa de 60s só para a UI local não ficar sem valor nenhum;
                // em produção (Cloudflare real) o webhook manda a duração de verdade.
                video.MarkReady(durationSeconds: 60, thumbnailUrl: null);
            }

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

            if (cookiesFilePath is not null && File.Exists(cookiesFilePath))
            {
                try { File.Delete(cookiesFilePath); }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Não foi possível apagar o arquivo temporário de cookies {CookiesFile}.", cookiesFilePath);
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
    private static async Task DownloadWithYtDlpAsync(
        string url, string outputPath, string? cookiesFilePath, CancellationToken ct)
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

        if (!string.IsNullOrWhiteSpace(cookiesFilePath))
        {
            psi.ArgumentList.Add("--cookies");
            psi.ArgumentList.Add(cookiesFilePath);
        }

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
