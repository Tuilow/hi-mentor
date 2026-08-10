using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tuilow.Finance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Infrastructure.Services;

/// <summary>
/// Implementação de <see cref="IAsaasSubaccountClient"/> contra a API oficial da Asaas (modelo
/// BaaS de subcontas — verificado em docs.asaas.com em ago/2026, ver anotações "CONFIRMADO NA
/// DOCUMENTAÇÃO" abaixo; itens sem essa marcação seguem a convenção de endpoint mais provável da
/// própria Asaas mas não foram verificados contra a página de referência exata — sinalizados
/// individualmente e também nas Pendências do relatório final).
///
/// Duas credenciais diferentes são usadas aqui, nunca confundidas:
///   - CreateSubaccountAsync usa a credencial da CONTA PAI da Tuilow ("Asaas:ApiKey" — o mesmo
///     valor já usado pelo modelo Legacy em AsaasPaymentService).
///   - Todas as demais operações (documentos, status, webhook) usam a API Key DA PRÓPRIA
///     subconta, recebida como parâmetro — a Asaas não expõe um mecanismo de "atuar como" a
///     subconta a partir da credencial da conta pai para essas chamadas (CONFIRMADO NA
///     DOCUMENTAÇÃO: "as chamadas subsequentes realizadas em nome da subconta deverão utilizar a
///     chave de API retornada na resposta da criação da conta").
/// </summary>
public sealed class AsaasSubaccountClient(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<AsaasSubaccountClient> logger
) : IAsaasSubaccountClient
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

    // ─── Criação (conta pai) ───────────────────────────────────────────────────

    public async Task<CreateAsaasSubaccountResult> CreateSubaccountAsync(CreateAsaasSubaccountRequest request, CancellationToken ct = default)
    {
        try
        {
            var parentApiKey = configuration["Asaas:ApiKey"];
            if (string.IsNullOrWhiteSpace(parentApiKey))
                throw new InvalidOperationException("Asaas:ApiKey (conta pai da Tuilow) não configurado — obrigatório para criar subcontas via BaaS.");

            using var client = CreateClient(parentApiKey);

            // CONFIRMADO NA DOCUMENTAÇÃO (docs.asaas.com/reference/criar-subconta): campos
            // obrigatórios name/email/cpfCnpj/mobilePhone/incomeValue/address/addressNumber/
            // province/postalCode; birthDate só para pessoa física; companyType só para pessoa
            // jurídica. Omitimos campos nulos/vazios do payload (mesma cautela já usada em
            // AsaasPaymentService.CreateOrGetCustomerAsync — a Asaas rejeita alguns campos
            // opcionais quando enviados como string vazia).
            var payload = new Dictionary<string, object?>
            {
                ["name"] = request.Name,
                ["email"] = request.Email,
                ["cpfCnpj"] = request.CpfCnpj,
                ["mobilePhone"] = request.MobilePhone,
                ["incomeValue"] = request.IncomeValue,
                ["address"] = request.Address,
                ["addressNumber"] = request.AddressNumber,
                ["province"] = request.Province,
                ["postalCode"] = request.PostalCode,
            };
            if (!string.IsNullOrWhiteSpace(request.Phone)) payload["phone"] = request.Phone;
            if (!string.IsNullOrWhiteSpace(request.AddressComplement)) payload["complement"] = request.AddressComplement;
            if (request.BirthDate is not null) payload["birthDate"] = request.BirthDate.Value.ToString("yyyy-MM-dd");
            if (!string.IsNullOrWhiteSpace(request.CompanyType)) payload["companyType"] = request.CompanyType;

            var json = JsonSerializer.Serialize(payload);
            var response = await client.PostAsync("accounts", new StringContent(json, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Asaas CreateSubaccount falhou [{Status}]: {Body}", (int)response.StatusCode, errorBody);
                return new CreateAsaasSubaccountResult(false, null, null, null,
                    ExtractErrorMessage(errorBody) ?? "Não foi possível criar a conta na Asaas. Tente novamente ou contate o suporte.");
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var accountId = root.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var walletId = root.TryGetProperty("walletId", out var walletEl) ? walletEl.GetString() : null;
            // A API Key só existe NESTA resposta — nunca mais é recuperável via API (só
            // regenerável por um fluxo manual no painel da Asaas, ver relatório final). O caller
            // (StartCreatorFinancialOnboardingCommandHandler) deve persistí-la criptografada
            // imediatamente e nunca logá-la.
            var apiKey = root.TryGetProperty("apiKey", out var apiKeyEl) ? apiKeyEl.GetString() : null;

            if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(apiKey))
            {
                logger.LogError("Asaas CreateSubaccount retornou 2xx sem id/apiKey no corpo (corpo omitido do log por poder conter a apiKey).");
                return new CreateAsaasSubaccountResult(false, null, null, null,
                    "A Asaas não retornou os dados esperados da conta criada. Contate o suporte antes de tentar novamente (evite duplicar a subconta).");
            }

            logger.LogInformation("Subconta Asaas criada: {AccountId}", accountId);
            return new CreateAsaasSubaccountResult(true, accountId, walletId, apiKey, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao criar subconta na Asaas.");
            return new CreateAsaasSubaccountResult(false, null, null, null, "Falha de comunicação com a Asaas. Tente novamente em instantes.");
        }
    }

    // ─── Documentos (credencial da subconta) ───────────────────────────────────

    public async Task<IReadOnlyList<AsaasOnboardingDocumentInfo>> GetPendingDocumentsAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(subaccountApiKeyPlaintext);

            // CONFIRMADO NA DOCUMENTAÇÃO: GET /v3/myAccount/documents, esperar >= 15s após criar
            // a conta (validação da Receita Federal) antes de consultar — a responsabilidade de
            // esperar fica no caller (StartCreatorFinancialOnboardingCommandHandler /
            // SyncCreatorOnboardingDocumentsCommandHandler), não aqui.
            var response = await client.GetAsync("myAccount/documents", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Asaas GetPendingDocuments falhou [{Status}]: {Body}", (int)response.StatusCode, body);
                return [];
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            var items = root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array
                    ? data
                    : root;

            if (items.ValueKind != JsonValueKind.Array) return [];

            var result = new List<AsaasOnboardingDocumentInfo>();
            foreach (var item in items.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                if (string.IsNullOrEmpty(id)) continue;

                result.Add(new AsaasOnboardingDocumentInfo(
                    id,
                    item.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "" : "",
                    item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "",
                    item.TryGetProperty("description", out var descEl) ? descEl.GetString() : null,
                    item.TryGetProperty("status", out var statusEl) ? statusEl.GetString() ?? "PENDING" : "PENDING",
                    item.TryGetProperty("onboardingUrl", out var urlEl) ? urlEl.GetString() : null));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao consultar documentos pendentes na Asaas.");
            return [];
        }
    }

    public async Task<bool> UploadDocumentAsync(string subaccountApiKeyPlaintext, string asaasDocumentId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(subaccountApiKeyPlaintext);

            // NÃO CONFIRMADO contra a página de referência exata de upload (não foi possível
            // consultar o schema multipart específico nesta sessão) — nome de campo "documentFile"
            // é o padrão mais comum em endpoints de upload da Asaas (ex.: anexos de cobrança), mas
            // deve ser validado contra sandbox real antes de produção (ver Pendências).
            using var multipart = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            multipart.Add(streamContent, "documentFile", fileName);

            var response = await client.PostAsync($"myAccount/documents/{asaasDocumentId}", multipart, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Asaas UploadDocument falhou [{Status}] para documento {DocumentId}: {Body}",
                    (int)response.StatusCode, asaasDocumentId, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao enviar documento {DocumentId} para a Asaas.", asaasDocumentId);
            return false;
        }
    }

    // ─── Status (credencial da subconta) ───────────────────────────────────────

    public async Task<AsaasAccountStatusInfo?> GetAccountStatusAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default)
    {
        try
        {
            using var client = CreateClient(subaccountApiKeyPlaintext);

            // NÃO CONFIRMADO contra uma página de referência dedicada — construído por analogia
            // com o objeto "accountStatus" do payload de webhook (commercialInfo/bankAccountInfo/
            // documentation/general), usado aqui só como fallback de refresh manual (o caminho
            // principal de atualização de status é o webhook, ver ProcessAsaasAccountStatusWebhookCommandHandler).
            var response = await client.GetAsync("myAccount/status", ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Asaas GetAccountStatus falhou [{Status}]: {Body}", (int)response.StatusCode, body);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? Field(string name) => root.TryGetProperty(name, out var el) ? el.GetString() : null;

            return new AsaasAccountStatusInfo(
                Field("general"), Field("documentation"), Field("commercialInfo"), Field("bankAccountInfo"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao consultar status da conta na Asaas.");
            return null;
        }
    }

    // ─── Webhook de status de conta (credencial da subconta) ───────────────────

    public async Task<bool> RegisterAccountStatusWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        using var client = CreateClient(subaccountApiKeyPlaintext);

        var webhookUrl = configuration["Asaas:AccountStatusWebhookUrl"];
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            logger.LogError("Asaas:AccountStatusWebhookUrl não configurado — não é possível registrar o webhook de status de conta.");
            return false;
        }

        var notificationEmail = configuration["Asaas:WebhookNotificationEmail"];
        if (string.IsNullOrWhiteSpace(notificationEmail))
            notificationEmail = "suporte@tuilow.com";

        // CONFIRMADO NA DOCUMENTAÇÃO (docs.asaas.com/docs/webhook-para-verificar-situacao-da-conta):
        // eventos ACCOUNT_STATUS_{BANK_ACCOUNT_INFO,COMMERCIAL_INFO,DOCUMENT,GENERAL_APPROVAL}_
        // {APPROVED,AWAITING_APPROVAL,PENDING,REJECTED} + COMMERCIAL_INFO_{EXPIRING_SOON,EXPIRED}.
        string[] events =
        [
            "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", "ACCOUNT_STATUS_GENERAL_APPROVAL_AWAITING_APPROVAL",
            "ACCOUNT_STATUS_GENERAL_APPROVAL_PENDING", "ACCOUNT_STATUS_GENERAL_APPROVAL_REJECTED",
            "ACCOUNT_STATUS_DOCUMENT_APPROVED", "ACCOUNT_STATUS_DOCUMENT_AWAITING_APPROVAL",
            "ACCOUNT_STATUS_DOCUMENT_PENDING", "ACCOUNT_STATUS_DOCUMENT_REJECTED",
            "ACCOUNT_STATUS_COMMERCIAL_INFO_APPROVED", "ACCOUNT_STATUS_COMMERCIAL_INFO_AWAITING_APPROVAL",
            "ACCOUNT_STATUS_COMMERCIAL_INFO_PENDING", "ACCOUNT_STATUS_COMMERCIAL_INFO_REJECTED",
            "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_APPROVED", "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_AWAITING_APPROVAL",
            "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_PENDING", "ACCOUNT_STATUS_BANK_ACCOUNT_INFO_REJECTED",
        ];

        return await AsaasWebhookRegistrar.RegisterOrUpdateAsync(
            client, webhookUrl, "Tuilow - Onboarding Financeiro", notificationEmail, webhookToken, events, logger, ct);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient("AsaasSubaccount");
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Remove("access_token");
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            client.DefaultRequestHeaders.Add("User-Agent", "Tuilow/1.0");
        return client;
    }

    private static string? ExtractErrorMessage(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                return errors[0].TryGetProperty("description", out var descEl) ? descEl.GetString() : null;
        }
        catch { /* ignora falha no parse */ }
        return null;
    }
}
