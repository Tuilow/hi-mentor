using System.Text;
using System.Text.Json;
using DogMaster.Application.Common.Interfaces;
using DogMaster.Application.Contexts.Subscription.Commands.ProcessWebhook;
using DogMaster.Domain.Contexts.Streaming.Interfaces;
using DogMaster.Domain.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DogMaster.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(
    ISender sender,
    IPaymentService paymentService,
    IVideoRepository videoRepository,
    IUnitOfWork uow,
    ILogger<WebhooksController> logger
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // ─── Asaas (pagamentos) ───────────────────────────────────────────────────

    /// <summary>Recebe eventos de pagamento do Asaas.</summary>
    [HttpPost("asaas")]
    public async Task<IActionResult> AsaasWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["asaas-access-token"].FirstOrDefault() ?? string.Empty;
        if (!paymentService.ValidateWebhookSignature(rawBody, signature))
            return Unauthorized(new { message = "Assinatura do webhook inválida." });

        var payload = JsonSerializer.Deserialize<AsaasWebhookPayload>(rawBody, JsonOpts);
        if (payload is null) return BadRequest();

        await sender.Send(new ProcessAsaasWebhookCommand(payload), ct);
        return Ok();
    }

    // ─── Cloudflare Stream (vídeos) ───────────────────────────────────────────

    /// <summary>
    /// Recebe notificação do Cloudflare Stream quando um vídeo termina de processar.
    /// Cloudflare chama este endpoint automaticamente — configure na aba Stream > Webhooks do painel.
    /// </summary>
    [HttpPost("cloudflare-stream")]
    public async Task<IActionResult> CloudflareStreamWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
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
