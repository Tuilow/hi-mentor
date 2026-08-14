using System.Text;
using System.Text.Json;
using HiMentor.Finance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiMentor.Finance.Infrastructure.Services;

/// <summary>
/// Chama a API da Asaas usando a API Key que o PRÓPRIO creator informou (nunca a API Key da
/// HiMentor) para: (1) validar a chave e confirmar que a conta está aprovada; (2) registrar (ou
/// atualizar/reativar, se já existir) nessa mesma conta um webhook de pagamentos apontando para
/// a HiMentor.
///
/// CONFIRMADO EM PRODUÇÃO (1): GET /v3/myAccount NÃO retorna "id" nem "walletId" para uma conta
/// comum (pessoa física ou jurídica fora do fluxo de subconta) -- o corpo real de resposta traz
/// apenas dados cadastrais (object, personType, company, cpfCnpj, name, status, endereço etc.,
/// ver AsaasAccountValidationResult). Esses dois campos só aparecem documentados no retorno de
/// POST /v3/accounts (criação de SUBCONTA), que não é o fluxo usado aqui (ver
/// CreatorAsaasAccount). Por isso:
/// - o "sucesso" da validação passou a ser o campo "status" == "APPROVED";
/// - como não existe um id de conta no retorno, usamos o cpfCnpj (único e sempre presente) como
///   AsaasAccountId;
/// - o walletId (usado só informativamente em CreatorAsaasAccount.WalletId -- o split de
///   pagamento sempre aponta para a walletId da própria HiMentor, nunca para a do creator) é
///   buscado em uma segunda chamada best-effort ao endpoint dedicado GET /v3/wallets; falha
///   nessa segunda chamada não bloqueia a conexão da conta.
///
/// CONFIRMADO EM PRODUÇÃO (2): o endpoint de criação de webhook é POST /v3/webhooks (PLURAL) --
/// a documentação atual da Asaas ("Create new Webhook via API") é explícita: "To create a
/// Webhook, use the endpoint: POST /v3/webhooks". O plural é o único documentado hoje.
///
/// CONFIRMADO EM PRODUÇÃO (3): a Asaas não permite dois webhooks cadastrados com a MESMA url na
/// MESMA conta -- se a conta do creator já tiver um webhook apontando para
/// Asaas:CreatorWebhookUrl (de uma tentativa anterior, ou porque a mesma conta também é usada
/// como conta própria da HiMentor no modelo legado), um POST /v3/webhooks novo falha. Por isso
/// RegisterWebhookAsync agora é idempotente: procura um webhook existente com a mesma url (GET
/// /v3/webhooks) e, se achar, faz PUT /v3/webhooks/{id} (reativando enabled/interrupted e
/// trocando o authToken) em vez de tentar criar um duplicado.
///
/// CONFIRMADO EM PRODUÇÃO (4): "Asaas:CreatorWebhookUrl" e "Asaas:WebhookNotificationEmail" no
/// appsettings.json são placeholders com STRING VAZIA (""), não chaves ausentes -- então, se a
/// variável de ambiente correspondente (Asaas__CreatorWebhookUrl / Asaas__WebhookNotificationEmail)
/// não estiver definida no ambiente de deploy, configuration[...] retorna "" (não null), e o
/// antigo "?? valorPadrao" NUNCA disparava (só dispara quando a chave está ausente, não quando
/// está vazia). Isso fazia a HiMentor mandar url/email em branco pro payload do webhook, e a Asaas
/// respondia 400 (invalid_url / invalid_email). Por isso agora tratamos string vazia/whitespace
/// como "não configurado" explicitamente.
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

            // GET /v3/myAccount (conta comum) não retorna "id"/"walletId" -- ver comentário da
            // classe. O sinal de conta válida/apta a vender é o campo "status".
            var status = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            if (!string.Equals(status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Conta Asaas do creator não está aprovada (status={Status}): {Body}", status, content);
                return new AsaasAccountValidationResult(false, null, null,
                    status is null
                        ? "A Asaas não retornou os dados esperados da conta. Tente novamente ou contate o suporte."
                        : $"Sua conta na Asaas ainda não está aprovada (status atual: {status}). Finalize o cadastro/verificação na Asaas e tente conectar novamente.");
            }

            var accountId = doc.RootElement.TryGetProperty("cpfCnpj", out var cpfEl) ? cpfEl.GetString() : null;
            if (string.IsNullOrEmpty(accountId))
            {
                logger.LogError("Asaas myAccount retornou 200 aprovado sem cpfCnpj: {Body}", content);
                return new AsaasAccountValidationResult(false, null, null,
                    "A Asaas não retornou os dados esperados da conta. Tente novamente ou contate o suporte.");
            }

            var walletId = await TryFetchWalletIdAsync(client, ct);

            return new AsaasAccountValidationResult(true, accountId, walletId ?? string.Empty, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao validar API Key de creator contra a Asaas.");
            return new AsaasAccountValidationResult(false, null, null, "Falha de comunicação com a Asaas. Tente novamente em instantes.");
        }
    }

    /// <summary>
    /// Busca o walletId da própria conta do creator em GET /v3/wallets (endpoint dedicado da
    /// Asaas para "qual é a walletId da conta dona desta API Key"). Best-effort: como o WalletId
    /// guardado em CreatorAsaasAccount é só informativo (o split nunca aponta para ele), qualquer
    /// falha aqui é logada como warning e NÃO impede a conexão da conta.
    /// </summary>
    private async Task<string?> TryFetchWalletIdAsync(HttpClient client, CancellationToken ct)
    {
        try
        {
            var response = await client.GetAsync("wallets/", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Não foi possível obter walletId informativo da conta do creator [{Status}]: {Body}", (int)response.StatusCode, body);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Cobre tanto um objeto único quanto o formato de listagem paginada padrão da Asaas
            // ({ object: "list", data: [...] }), já que a doc pública não detalha o schema exato.
            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array)
            {
                if (data.GetArrayLength() == 0) return null;
                root = data[0];
            }

            return root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao buscar walletId informativo da conta do creator (não bloqueia a conexão).");
            return null;
        }
    }

    public async Task<bool> RegisterWebhookAsync(string apiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(apiKeyPlaintext);

            var webhookUrl = configuration["Asaas:CreatorWebhookUrl"];
            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new InvalidOperationException(
                    "Asaas:CreatorWebhookUrl não configurado (variável de ambiente Asaas__CreatorWebhookUrl vazia ou ausente).");

            var notificationEmail = configuration["Asaas:WebhookNotificationEmail"];
            if (string.IsNullOrWhiteSpace(notificationEmail))
                notificationEmail = "suporte@himentor.com";

            var payload = new
            {
                name = "HiMentor - Marketplace",
                url = webhookUrl,
                email = notificationEmail,
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

            // Idempotente: se a conta já tem um webhook cadastrado para essa mesma url (tentativa
            // anterior, ou porque essa conta Asaas também é a conta própria da HiMentor no modelo
            // legado), ATUALIZA/reativa esse webhook em vez de tentar criar um duplicado -- a
            // Asaas rejeita um segundo webhook com a mesma url na mesma conta.
            var existingId = await TryFindWebhookIdByUrlAsync(client, webhookUrl, ct);

            var response = existingId is null
                ? await client.PostAsync("webhooks", new StringContent(json, Encoding.UTF8, "application/json"), ct)
                : await client.PutAsync($"webhooks/{existingId}", new StringContent(json, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Falha ao {Operation} webhook na conta do creator [{Status}]: {Body}",
                    existingId is null ? "registrar" : "atualizar", (int)response.StatusCode, body);
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

    /// <summary>
    /// Procura, em GET /v3/webhooks, um webhook já cadastrado nesta conta cuja url bata com
    /// webhookUrl (comparação tolerante a barra final). Best-effort: se a listagem falhar,
    /// assume que não existe e deixa RegisterWebhookAsync tentar criar via POST normalmente.
    /// </summary>
    private async Task<string?> TryFindWebhookIdByUrlAsync(HttpClient client, string webhookUrl, CancellationToken ct)
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
            logger.LogWarning(ex, "Falha ao listar webhooks existentes da conta do creator (tentará criar um novo).");
            return null;
        }
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient("AsaasOnboarding");
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "HiMentor/1.0");
        return client;
    }
}
