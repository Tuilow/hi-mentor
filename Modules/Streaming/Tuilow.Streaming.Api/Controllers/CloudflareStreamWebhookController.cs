using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    IConfiguration configuration,
    ILogger<CloudflareStreamWebhookController> logger
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    // Achado A6 da avaliação: janela de tolerância pro timestamp do header Webhook-Signature —
    // sem isso, uma assinatura capturada (ex.: em log de proxy) continuaria válida pra sempre.
    private static readonly TimeSpan SignatureTolerance = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Recebe notificação do Cloudflare Stream quando um vídeo termina de processar.
    /// Cloudflare chama este endpoint automaticamente — configure na aba Stream > Webhooks do
    /// painel (a URL cadastrada lá gera o Webhook-Signature secret usado aqui).
    /// </summary>
    [HttpPost("cloudflare-stream")]
    public async Task<IActionResult> CloudflareStreamWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, System.Text.Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        // Achado A6 da avaliação (ALTO): ao contrário do webhook da Asaas, este endpoint
        // processava qualquer POST sem checar nenhuma assinatura — bastava adivinhar/conhecer
        // um uid de vídeo real para forjar um "stream.video.finished" e marcar qualquer vídeo
        // como pronto. Cloudflare assina com HMAC-SHA256 sobre "{time}.{body}" no header
        // "Webhook-Signature: time=<unix>,sig1=<hex>" — valida em tempo constante, com janela
        // de tolerância pro timestamp (replay de uma assinatura antiga vazada).
        if (!ValidateSignature(rawBody))
            return Unauthorized(new { message = "Assinatura do webhook inválida." });

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

    private bool ValidateSignature(string rawBody)
    {
        var webhookSecret = configuration["Cloudflare:StreamWebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
        {
            // Sem secret configurado: Program.cs falha o startup fora de Development quando
            // Cloudflare:StreamWebhookSecret vem vazio (mesmo padrão do Asaas:WebhookSecret),
            // então isto só deveria ser alcançado em desenvolvimento local.
            logger.LogWarning(
                "Cloudflare:StreamWebhookSecret vazio — aceitando webhook sem validação (esperado só em Development).");
            return true;
        }

        var header = Request.Headers["Webhook-Signature"].FirstOrDefault();
        if (string.IsNullOrEmpty(header)) return false;

        // Formato: "time=1234567890,sig1=abcdef..."
        string? timeStr = null, sig1 = null;
        foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            if (kv[0].Trim() == "time") timeStr = kv[1].Trim();
            else if (kv[0].Trim() == "sig1") sig1 = kv[1].Trim();
        }

        if (timeStr is null || sig1 is null) return false;
        if (!long.TryParse(timeStr, out var timeUnix)) return false;

        var signedAt = DateTimeOffset.FromUnixTimeSeconds(timeUnix);
        if (DateTimeOffset.UtcNow - signedAt > SignatureTolerance
            || signedAt - DateTimeOffset.UtcNow > SignatureTolerance)
        {
            logger.LogWarning("Webhook-Signature fora da janela de tolerância ({SignedAt}).", signedAt);
            return false;
        }

        var expectedHash = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret))
            .ComputeHash(Encoding.UTF8.GetBytes($"{timeStr}.{rawBody}"));
        var expected = Convert.ToHexString(expectedHash).ToLowerInvariant();

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(sig1.ToLowerInvariant());

        return expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
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
