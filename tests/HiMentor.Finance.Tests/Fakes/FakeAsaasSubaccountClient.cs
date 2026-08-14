using HiMentor.Finance.Application.Interfaces;

namespace HiMentor.Finance.Tests.Fakes;

/// <summary>
/// Fake configurável do cliente Asaas — nenhum teste de handler faz HTTP de verdade (sem suíte de
/// integração neste primeiro pass, ver Pendências do relatório final). Conta chamadas para provar
/// idempotência: uma segunda execução do comando de onboarding para o mesmo criador (já com
/// AsaasAccountId preenchido) nunca deve incrementar CreateSubaccountCallCount.
/// </summary>
public sealed class FakeAsaasSubaccountClient : IAsaasSubaccountClient
{
    public int CreateSubaccountCallCount { get; private set; }
    public int RegisterWebhookCallCount { get; private set; }

    public bool NextCreateShouldSucceed { get; set; } = true;
    public string NextAsaasAccountId { get; set; } = "acc_fake_123";
    public string NextWalletId { get; set; } = "wallet_fake_123";
    public string NextApiKey { get; set; } = "$aact_fake_api_key_never_logged";
    public string? NextErrorMessage { get; set; }
    public bool NextWebhookRegistrationShouldSucceed { get; set; } = true;

    public int GetPendingDocumentsCallCount { get; private set; }

    /// <summary>
    /// Configurável pelos testes — simula a resposta HTTP 200 real de GET /v3/myAccount/documents
    /// (ver bug de persistência corrigido nesta sessão: a integração com a Asaas em si sempre
    /// funcionou, o problema era só na gravação local depois desta chamada). Vazia por padrão
    /// (mesmo comportamento anterior, preserva os testes existentes).
    /// </summary>
    public IReadOnlyList<AsaasOnboardingDocumentInfo> NextPendingDocuments { get; set; } = [];

    public Task<CreateAsaasSubaccountResult> CreateSubaccountAsync(CreateAsaasSubaccountRequest request, CancellationToken ct = default)
    {
        CreateSubaccountCallCount++;

        return Task.FromResult(NextCreateShouldSucceed
            ? new CreateAsaasSubaccountResult(true, NextAsaasAccountId, NextWalletId, NextApiKey, null)
            : new CreateAsaasSubaccountResult(false, null, null, null, NextErrorMessage ?? "Falha simulada na criação da subconta."));
    }

    public Task<IReadOnlyList<AsaasOnboardingDocumentInfo>> GetPendingDocumentsAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default)
    {
        GetPendingDocumentsCallCount++;
        return Task.FromResult(NextPendingDocuments);
    }

    public int UploadDocumentCallCount { get; private set; }
    public bool NextUploadShouldSucceed { get; set; } = true;

    public Task<bool> UploadDocumentAsync(string subaccountApiKeyPlaintext, string asaasDocumentId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        UploadDocumentCallCount++;
        return Task.FromResult(NextUploadShouldSucceed);
    }

    public int GetAccountStatusCallCount { get; private set; }

    /// <summary>
    /// Configurável pelos testes -- simula GET /myAccount/status. Null por padrão (mesmo
    /// comportamento anterior: falha/sem dado, ver SyncCreatorOnboardingAccountStatusCommandHandler
    /// tratando isso como best-effort sem propagar).
    /// </summary>
    public AsaasAccountStatusInfo? NextAccountStatus { get; set; }

    public Task<AsaasAccountStatusInfo?> GetAccountStatusAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default)
    {
        GetAccountStatusCallCount++;
        return Task.FromResult(NextAccountStatus);
    }

    public Task<bool> RegisterAccountStatusWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        RegisterWebhookCallCount++;
        return Task.FromResult(NextWebhookRegistrationShouldSucceed);
    }

    public int RegisterPaymentWebhookCallCount { get; private set; }
    public bool NextPaymentWebhookRegistrationShouldSucceed { get; set; } = true;

    public Task<bool> RegisterPaymentWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        RegisterPaymentWebhookCallCount++;
        return Task.FromResult(NextPaymentWebhookRegistrationShouldSucceed);
    }
}
