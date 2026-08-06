using System.Text;
using System.Text.Json;
using Tuilow.Finance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Infrastructure.Services;

/// <summary>
/// Chama a API da Asaas usando a API Key que o PRÓPRIO creator informou (nunca a API Key da
/// Tuilow) para: (1) validar a chave e descobrir o id da conta e a walletId associados a ela;
/// (2) registrar nessa mesma conta um webhook de pagamentos apontando para a Tuilow.
///
/// PONTO DE ATENÇÃO (ver IAsaasAccountOnboardingService): os dois endpoints usados aqui
/// (GET /v3/myAccount e POST /v3/webhook) são os documentados atualmente pela Asaas para,
/// respectivamente, consultar os dados da conta associada à API Key da chamada e configurar um
/// webhook via API — mas a documentação pública consultada nesta sessão não confirma
/// explicitamente o schema de resposta de "myAccount" nem o payload exato de "webhook" para
/// este cenário (conta comum do creator, fora do fluxo de subconta). Antes de habilitar
/// Asaas:MarketplaceSplitEnabled em produção, validar esta chamada com uma API Key real de
/// Sandbox e ajustar os nomes de campo abaixo se necessário.
/// </summary>
public sealed class AsaasAccountOnboardingService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AsaasAccountOnboardingService> logger
) : IAsaasAccountOnboardingService
{
    private string BaseUrl
    {
        get
        {
            var baseUrl = configuration["Asaas:BaseUrl"] ?? "https://api-sandbox.asaas.com/v3";
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            return baseUrl;
        }
    }

    public async Task<AsaasAccountValidationResult> ValidateAndFetchAccountAsync(string apiKeyPlaintext, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(apiKeyPlaintext);
            var response = await client.GetAsync("myAccount", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Validação de API Key de creator falhou [{Status}]: {Body}", (int)response.StatusCode, errorBody);
                return new AsaasAccountValidationResult(false, null, null,
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "API Key inválida ou sem permissão."
                        : "Não foi possível validar a conta na Asaas.");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);

            var accountId = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var walletId = doc.RootElement.TryGetProperty("walletId", out var walletEl) ? walletEl.GetString() : null;

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(walletId))
            {
                logger.LogError("Asaas myAccount retornou 200 sem id/walletId: {Body}", content);
                return new AsaasAccountValidationResult(false, null, null,
                    "A Asaas não retornou os dados esperados da conta (id/walletId). Tente novamente ou contate o suporte.");
            }

            return new AsaasAccountValidationResult(true, accountId, walletId, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao validar API Key de creator contra a Asaas.");
            return new AsaasAccountValidationResult(false, null, null, "Falha de comunicação com a Asaas. Tente novamente em instantes.");
        }
    }

    public async Task<bool> RegisterWebhookAsync(string apiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(apiKeyPlaintext);

            var webhookUrl = configuration["Asaas:CreatorWebhookUrl"]
                ?? throw new InvalidOperationException("Asaas:CreatorWebhookUrl não configurado.");

            var payload = new
            {
                name = "Tuilow - Marketplace",
                url = webhookUrl,
                email = configuration["Asaas:WebhookNotificationEmail"] ?? "suporte@tuilow.com",
                enabled = true,
                interrupted = false,
                apiVersion = 3,
                authToken = webhookToken,
                sendType = "SEQUENTIALLY",
                events = new[]
                {
                    "PAYMENT_RECEIVED", "PAYMENT_CONFIRMED", "PAYMENT_OVERDUE", "PAYMENT_DELETED",
                    "PAYMENT_REFUNDED", "PAYMENT_SPLIT_DIVERGENCE_BLOCK", "PAYMENT_SPLIT_DIVERGENCE_BLOCK_FINISHED"
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync("webhook", new StringContent(json, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Falha ao registrar webhook na conta do creator [{Status}]: {Body}", (int)response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar webhook na conta do creator.");
            return false;
        }
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient("AsaasOnboarding");
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "Tuilow/1.0");
        return client;
    }
}
