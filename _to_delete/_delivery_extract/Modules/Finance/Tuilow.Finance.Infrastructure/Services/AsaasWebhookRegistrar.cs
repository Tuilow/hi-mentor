using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Infrastructure.Services;

/// <summary>
/// Idioma idempotente de registro de webhook (POST /v3/webhooks, ou PUT /v3/webhooks/{id} se já
/// existir um com a mesma url) — extraído aqui para ser reaproveitado por
/// <see cref="AsaasSubaccountClient"/> sem duplicar a lógica já provada em produção por
/// <see cref="AsaasAccountOnboardingService.RegisterWebhookAsync"/>. Deliberadamente não usado
/// por AsaasAccountOnboardingService (código legado, funcionando — não mexer sem necessidade).
/// </summary>
internal static class AsaasWebhookRegistrar
{
    public static async Task<bool> RegisterOrUpdateAsync(
        HttpClient client, string webhookUrl, string webhookName, string notificationEmail,
        string webhookToken, string[] events, ILogger logger, CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                name = webhookName,
                url = webhookUrl,
                email = notificationEmail,
                enabled = true,
                interrupted = false,
                apiVersion = 3,
                authToken = webhookToken,
                sendType = "SEQUENTIALLY",
                events
            };

            var json = JsonSerializer.Serialize(payload);

            var existingId = await TryFindWebhookIdByUrlAsync(client, webhookUrl, logger, ct);

            var response = existingId is null
                ? await client.PostAsync("webhooks", new StringContent(json, Encoding.UTF8, "application/json"), ct)
                : await client.PutAsync($"webhooks/{existingId}", new StringContent(json, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Falha ao {Operation} webhook '{Name}' [{Status}]: {Body}",
                    existingId is null ? "registrar" : "atualizar", webhookName, (int)response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar webhook '{Name}'.", webhookName);
            return false;
        }
    }

    private static async Task<string?> TryFindWebhookIdByUrlAsync(HttpClient client, string webhookUrl, ILogger logger, CancellationToken ct)
    {
        try
        {
            var response = await client.GetAsync("webhooks?limit=100", ct);
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var items = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array
                    ? data
                    : root;

            if (items.ValueKind != JsonValueKind.Array) return null;

            var normalizedTarget = webhookUrl.TrimEnd('/');
            foreach (var item in items.EnumerateArray())
            {
                var url = item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
                if (url is not null && string.Equals(url.TrimEnd('/'), normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    return item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao listar webhooks existentes (tentará criar um novo).");
            return null;
        }
    }
}
