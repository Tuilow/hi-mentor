using Tuilow.Finance.Application.Interfaces;

namespace Tuilow.Finance.Tests.Fakes;

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

    public Task<CreateAsaasSubaccountResult> CreateSubaccountAsync(CreateAsaasSubaccountRequest request, CancellationToken ct = default)
    {
        CreateSubaccountCallCount++;

        return Task.FromResult(NextCreateShouldSucceed
            ? new CreateAsaasSubaccountResult(true, NextAsaasAccountId, NextWalletId, NextApiKey, null)
            : new CreateAsaasSubaccountResult(false, null, null, null, NextErrorMessage ?? "Falha simulada na criação da subconta."));
    }

    public Task<IReadOnlyList<AsaasOnboardingDocumentInfo>> GetPendingDocumentsAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AsaasOnboardingDocumentInfo>>([]);

    public Task<bool> UploadDocumentAsync(string subaccountApiKeyPlaintext, string asaasDocumentId, Stream fileStream, string fileName, string contentType, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<AsaasAccountStatusInfo?> GetAccountStatusAsync(string subaccountApiKeyPlaintext, CancellationToken ct = default) =>
        Task.FromResult<AsaasAccountStatusInfo?>(null);

    public Task<bool> RegisterAccountStatusWebhookAsync(string subaccountApiKeyPlaintext, string webhookToken, CancellationToken ct = default)
    {
        RegisterWebhookCallCount++;
        return Task.FromResult(NextWebhookRegistrationShouldSucceed);
    }
}
