namespace Tuilow.Finance.Application.Interfaces;

public sealed record CreateAsaasSubaccountRequest(
    string Name, string Email, string CpfCnpj, string MobilePhone, string? Phone,
    decimal IncomeValue, string Address, string AddressNumber, string? AddressComplement,
    string Province, string PostalCode, DateOnly? BirthDate, string? CompanyType
);

public sealed record CreateAsaasSubaccountResult(bool Success, string? AsaasAccountId, string? WalletId, string? ApiKey, string? ErrorMessage);

/// <summary>Espelha 1:1 um item de GET /v3/myAccount/documents — Status/Type/OnboardingUrl exatamente como a Asaas retorna, nunca reinterpretados aqui.</summary>
public sealed record AsaasOnboardingDocumentInfo(string Id, string Type, string Title, string? Description, string Status, string? OnboardingUrl);

public sealed record AsaasAccountStatusInfo(string? GeneralStatus, string? DocumentationStatus, string? CommercialInfoStatus, string? BankAccountInfoStatus);

/// <summary>
/// Encapsula toda a comunicação HTTP com a API da Asaas para o modelo de subconta BaaS (criação,
/// documentos, status, webhook de conta) — nenhum Controller/Application chama a Asaas
/// diretamente, sempre por aqui (ver item 9 do briefing de onboarding financeiro). Distinto de
/// <see cref="IAsaasAccountOnboardingService"/> (que continua servindo só o modelo legado de
/// "cole sua API Key", ver CreatorAsaasAccount).
/// </summary>
public interface IAsaasSubaccountClient
{
    /// <summary>POST /v3/accounts com a credencial da conta pai (Asaas:ApiKey) — cria a subconta do criador na Asaas. O ApiKey da subconta retornado aqui só existe em memória neste ponto; o caller deve persistí-lo criptografado imediatamente (ver ISecretProtector) e nunca logá-lo.</summary>
    Task<CreateAsaasSubaccountResult> CreateSubaccountAsync(CreateAsaasSubaccountRequest request, CancellationToken ct = default);

    /// <summary>GET /v3/myAccount/documents com a API Key DA PRÓPRIA subconta — lista os documentos pendentes/enviados para o onboarding/KYC.</summary>
    Task<IReadOnlyList<AsaasOnboardingDocumentInfo>> GetPendingDocumentsAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default);

    /// <summary>POST /v3/myAccount/documents/{id} — só válido para documentos sem OnboardingUrl (ver CreatorAsaasOnboardingDocument). O arquivo é sempre um proxy de stream direto para a Asaas, nunca persistido pela Tuilow.</summary>
    Task<bool> UploadDocumentAsync(string subaccountApiKeyPlaintext, string asaasDocumentId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Fallback de polling/refresh manual (admin ou sincronização periódica) — status agregado da conta, sem depender só do webhook.</summary>
    Task<AsaasAccountStatusInfo?> GetAccountStatusAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default);

    /// <summary>Registra (ou atualiza/reativa, se já existir para a mesma url) o webhook de status de conta (ACCOUNT_STATUS_*) na subconta — mesmo idioma idempotente já provado em IAsaasAccountOnboardingService.RegisterWebhookAsync.</summary>
    Task<bool> RegisterAccountStatusWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default);

    /// <summary>
    /// Registra (ou atualiza/reativa) o webhook de PAGAMENTO (PAYMENT_*) na subconta — necessário
    /// para o marketplace de split (Sales.AsaasMarketplacePaymentService) funcionar de verdade:
    /// sem isso, uma cobrança criada direto na conta do criador nunca gera nenhum evento de volta
    /// pra Tuilow (a compra fica presa em "Pending" para sempre). Reaproveita a mesma url já
    /// configurada para o modelo legado (Asaas:CreatorWebhookUrl, aponta para
    /// Sales.AsaasWebhookController — api/v1/webhooks/asaas) e o MESMO token de
    /// RegisterAccountStatusWebhookAsync (WebhookTokenHash é um único hash por subconta,
    /// compartilhado pelos dois webhooks — o token em si não é específico de nenhuma url).
    /// </summary>
    Task<bool> RegisterPaymentWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default);
}
