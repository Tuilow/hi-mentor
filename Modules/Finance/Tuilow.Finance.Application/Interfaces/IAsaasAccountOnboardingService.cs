namespace Tuilow.Finance.Application.Interfaces;

public sealed record AsaasAccountValidationResult(bool Success, string? AsaasAccountId, string? WalletId, string? ErrorMessage);

/// <summary>
/// Valida a API Key que um creator informa (conta Asaas PROPRIA dele -- pessoa fisica ou
/// juridica, fora do controle da Tuilow, ver CreatorAsaasAccount) e registra nessa conta um
/// webhook de pagamentos apontando para a Tuilow. Como nao e uma subconta que a Tuilow
/// administra, essa e a unica forma de saber quando uma cobranca criada nela e paga.
///
/// IMPORTANTE (ponto de atencao documentado no relatorio final): o endpoint exato usado pela
/// implementacao (Finance.Infrastructure) para "quais sao os dados da conta desta API Key"
/// (GET /v3/myAccount) e "registrar webhook" (POST /v3/webhook) sao os documentados atualmente
/// pela Asaas para consulta de conta propria e configuracao de webhook via API -- validar contra
/// uma chamada real de Sandbox antes de habilitar em producao (ver Asaas:MarketplaceSplitEnabled),
/// já que a documentação pública não confirma explicitamente o schema de resposta desses dois
/// endpoints para este cenário específico (conta comum, não subconta).
/// </summary>
public interface IAsaasAccountOnboardingService
{
    Task<AsaasAccountValidationResult> ValidateAndFetchAccountAsync(string apiKeyPlaintext, CancellationToken ct = default);

    /// <summary>Registra (application/json, evento PAYMENT) o webhook de pagamentos na conta do creator, autenticado com webhookToken.</summary>
    Task<bool> RegisterWebhookAsync(string apiKeyPlaintext, string webhookToken, CancellationToken ct = default);
}
