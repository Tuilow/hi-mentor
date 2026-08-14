using System.Text;
using System.Text.Json;
using HiMentor.Sales.Application.Commands.ProcessWebhook;
using HiMentor.Sales.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.Sales.Api.Controllers;

/// <summary>
/// Split de HiMentor.API.Controllers.WebhooksController — apenas a parte de pagamentos (Asaas).
/// O webhook do Cloudflare Stream permanece no HiMentor.API legado (contexto Streaming ainda
/// não migrado para Modules/).
///
/// Recebe webhooks de DUAS origens diferentes no mesmo endpoint: a conta Asaas da própria
/// HiMentor (assinaturas e compras Legacy) e a conta Asaas de qualquer creator que tenha
/// conectado o marketplace de split (cada um com seu próprio token de webhook) — ver
/// IAsaasWebhookAuthenticator, que tenta as duas formas de autenticação.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
public sealed class AsaasWebhookController(
    ISender sender,
    IAsaasWebhookAuthenticator webhookAuthenticator
) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>Recebe eventos de pagamento do Asaas (conta da HiMentor ou conta de um creator no marketplace).</summary>
    [HttpPost("asaas")]
    public async Task<IActionResult> AsaasWebhook(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var accessToken = Request.Headers["asaas-access-token"].FirstOrDefault() ?? string.Empty;
        var auth = await webhookAuthenticator.AuthenticateAsync(accessToken, ct);
        if (!auth.IsValid)
            return Unauthorized(new { message = "Assinatura do webhook inválida." });

        var payload = JsonSerializer.Deserialize<AsaasWebhookPayload>(rawBody, JsonOpts);
        if (payload is null) return BadRequest();

        await sender.Send(new ProcessAsaasWebhookCommand(payload, auth.CreatorAsaasAccountId), ct);
        return Ok();
    }
}
