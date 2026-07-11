using System.Text;
using System.Text.Json;
using Tuilow.Sales.Application.Commands.ProcessWebhook;
using Tuilow.Sales.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Sales.Api.Controllers;

/// <summary>
/// Split de Tuilow.API.Controllers.WebhooksController — apenas a parte de pagamentos (Asaas).
/// O webhook do Cloudflare Stream permanece no Tuilow.API legado (contexto Streaming ainda
/// não migrado para Modules/).
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public sealed class AsaasWebhookController(
    ISender sender,
    IPaymentService paymentService
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

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
}
