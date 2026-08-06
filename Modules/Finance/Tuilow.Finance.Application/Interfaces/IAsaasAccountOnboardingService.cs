namespace Tuilow.Finance.Application.Interfaces;

public sealed record AsaasAccountValidationResult(bool Success, string? AsaasAccountId, string? WalletId, string? ErrorMessage);

/// <summary>
/// Valida a API Key que um creator informa (conta Asaas PROPRIA dele -- pessoa fisica ou
/// juridica, fora do controle da Tuilow, ver CreatorAsaasAccount) e registra nessa conta um
/// webhook de pagamentos apontando para a Tuilow. Como nao e uma subconta que a Tuilow
/// administra, essa e a unica forma de saber quando uma cobranca criada nela e paga.
///
/// IMPORTANTE (ponto de atencao do relatorio final, agora RESOLVIDO com uma chamada real em
/// producao): GET /v3/myAccount para uma conta comum (nao subconta) nao retorna "id" nem
/// "walletId" -- so dados cadastrais (status, cpfCnpj, name, endereco etc.). A implementacao
/// (Finance.Infrastructure) usa "status" == APPROVED para validar a conta, "cpfCnpj" como
/// AsaasAccountId (a Asaas nao expoe um id de conta nesse endpoint) e busca a walletId
/// (informativa, ver CreatorAsaasAccount.WalletId) numa segunda chamada best-effort a
/// GET /v3/wallets.
/// </summary>
public interface IAsaasAccountOnboardingService
{
    Task<AsaasAccountValidationResult> ValidateAndFetchAccountAsync(string apiKeyPlaintext, CancellationToken ct = default);

    /// <summary>Registra (application/json, evento PAYMENT) o webhook de pagamentos na conta do creator, autenticado com webhookToken.</summary>
    Task<bool> RegisterWebhookAsync(string apiKeyPlaintext, string webhookToken, CancellationToken ct = default);
}
