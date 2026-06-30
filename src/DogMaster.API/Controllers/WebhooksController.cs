using System.Text;
using DogMaster.Application.Common.Interfaces;
using DogMaster.Application.Contexts.Subscription.Commands.ProcessWebhook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DogMaster.API.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public sealed class WebhooksController(ISender sender, IPaymentService paymentService) : ControllerBase
{
    /// <summary>Recebe eventos de pagamento do Asaas.</summary>
    [HttpPost("asaas")]
    public async Task<IActionResult> AsaasWebhook(CancellationToken ct)
    {
        // Lê raw body para validação de assinatura
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var signature = Request.Headers["asaas-access-token"].FirstOrDefault() ?? string.Empty;
        if (!paymentService.ValidateWebhookSignature(rawBody, signature))
            return Unauthorized(new { message = "Assinatura do webhook inválida." });

        var payload = System.Text.Json.JsonSerializer.Deserialize<AsaasWebhookPayload>(
            rawBody, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (payload is null) return BadRequest();

        await sender.Send(new ProcessAsaasWebhookCommand(payload), ct);
        return Ok();
    }
}
