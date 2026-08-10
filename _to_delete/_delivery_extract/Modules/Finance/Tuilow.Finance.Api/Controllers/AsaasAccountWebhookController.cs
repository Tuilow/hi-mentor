using System.Text;
using System.Text.Json;
using Tuilow.Finance.Application.Commands.ProcessAsaasAccountStatusWebhook;
using Tuilow.Finance.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Finance.Api.Controllers;

/// <summary>
/// Webhook de status de conta (ACCOUNT_STATUS_*) das subcontas criadas pela Tuilow via BaaS — rota
/// isolada do webhook de pagamento (Sales.Api.AsaasWebhookController, que trata só PAYMENT_*):
/// o payload tem formato completamente diferente ("account"/"accountStatus", não "payment"), e
/// mantê-los separados evita qualquer risco de tocar no caminho de pagamento já validado em
/// produção enquanto este é novo.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public sealed class AsaasAccountWebhookController(
    ISender sender,
    IAsaasAccountStatusWebhookAuthenticator webhookAuthenticator
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    [HttpPost("asaas-account-status")]
    public async Task<IActionResult> AccountStatusWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var accessToken = Request.Headers["asaas-access-token"].FirstOrDefault() ?? string.Empty;
        var auth = await webhookAuthenticator.AuthenticateAsync(accessToken, ct);
        if (!auth.IsValid)
            return Unauthorized(new { message = "Assinatura do webhook inválida." });

        var payload = JsonSerializer.Deserialize<AsaasAccountStatusPayload>(rawBody, JsonOpts);
        if (payload is null) return BadRequest();

        await sender.Send(new ProcessAsaasAccountStatusWebhookCommand(payload), ct);
        return Ok();
    }
}
