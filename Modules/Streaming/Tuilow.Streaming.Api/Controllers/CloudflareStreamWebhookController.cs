using System.Text.Json;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Tuilow.Streaming.Api.Controllers;

/// <summary>
/// Split de Tuilow.API.Controllers.WebhooksController — só a parte do Cloudflare Stream
/// (o webhook do Asaas foi para Tuilow.Sales.Api.Controllers.AsaasWebhookController).
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public sealed class CloudflareStreamWebhookController(
    IVideoRepository videoRepository,
    IUnitOfWork uow,
    ILogger<CloudflareStreamWebhookController> logger
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Recebe notificação do Cloudflare Stream quando um vídeo termina de processar.
    /// Cloudflare chama este endpoint automaticamente — configure na aba Stream > Webhooks do painel.
    /// </summary>
    [HttpPost("cloudflare-stream")]
    public async Task<IActionResult> CloudflareStreamWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        CloudflareStreamEvent? evt;
        try { evt = JsonSerializer.Deserialize<CloudflareStreamEvent>(rawBody, JsonOpts); }
        catch { return BadRequest(); }

        if (evt is null) return BadRequest();

        logger.LogInformation("Cloudflare Stream webhook: {Event} uid={Uid}", evt.Event, evt.Uid);

        // Cloudflare envia "stream.video.finished" quando o processamento termina
        if (evt.Event == "stream.video.finished")
        {
            var video = await videoRepository.GetByCloudflareIdAsync(evt.Uid, ct);
            if (video is null)
            {
                logger.LogWarning("Vídeo com Cloudflare uid={Uid} não encontrado.", evt.Uid);
                return Ok(); // Retorna 200 para Cloudflare não retentar
            }

            var duration = evt.ReadyToStream && evt.Duration.HasValue
                ? (int)Math.Ceiling(evt.Duration.Value)
                : 0;

            video.MarkReady(duration, evt.Thumbnail);
            videoRepository.Update(video);
            await uow.SaveChangesAsync(ct);

            logger.LogInformation("Vídeo {VideoId} marcado como pronto. Duração: {Duration}s", video.Id, duration);
        }

        return Ok();
    }
}

// ─── Cloudflare Stream event payload ──────────────────────────────────────────
public sealed class CloudflareStreamEvent
{
    public string Event { get; init; } = string.Empty;
    public string Uid   { get; init; } = string.Empty;
    public bool ReadyToStream { get; init; }
    public double? Duration   { get; init; }
    public string? Thumbnail  { get; init; }
}
