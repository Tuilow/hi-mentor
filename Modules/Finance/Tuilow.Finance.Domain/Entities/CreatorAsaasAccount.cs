using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Finance.Domain.Enums;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Conexao financeira de um creator para o marketplace de split de pagamentos (venda de curso
/// com o CREATOR como emissor da cobranca, ver decisao de arquitetura registrada no relatorio
/// final desta feature).
///
/// IMPORTANTE -- por que isto NAO e uma "subconta" criada pela Tuilow via API (POST
/// /v3/accounts): a documentacao oficial da Asaas exige pessoa juridica (companyType
/// MEI/LIMITED/INDIVIDUAL-empresarial/ASSOCIATION) para criar subconta via API -- "contas de
/// pessoa fisica (CPF) nao podem criar subconta". Como uma fatia relevante dos creators da
/// Tuilow sao autonomos sem CNPJ, o modelo escolhido (decisao do dono do produto) foi: o proprio
/// creator cria/ja possui uma conta Asaas comum (pessoa fisica ou juridica, fora da API de
/// subcontas), gera uma API Key nessa conta e conecta aqui. A Tuilow usa essa API Key para criar
/// a cobranca DIRETAMENTE na conta do creator (ele e o emissor/vendedor de fato, inclusive para
/// fins de nota fiscal) com um split apontando para a walletId da Tuilow (comissao). Isso nao
/// exige subconta nenhuma -- so uma API Key com permissao de criar cobranca, cliente e webhook.
///
/// AsaasAccountId/WalletId/ApiKeyEncrypted sao capturados na conexao (ConnectAccount) validando
/// a API Key contra a propria API da Asaas. ApiKeyEncrypted NUNCA e exposto fora da
/// Infrastructure (ver ISecretProtector) -- Application/Api so enxergam Status/WalletId/flags.
/// </summary>
public sealed class CreatorAsaasAccount : AggregateRoot
{
    public Guid CreatorId { get; private set; }

    /// <summary>Identificador da conta na Asaas (retornado por GET /myAccount ao validar a API Key).</summary>
    public string AsaasAccountId { get; private set; } = string.Empty;

    /// <summary>walletId da conta do creator -- usado apenas informativamente aqui (o split aponta para a walletId da TUILOW, nao esta).</summary>
    public string WalletId { get; private set; } = string.Empty;

    /// <summary>API Key da conta do creator, protegida via ISecretProtector -- nunca texto puro.</summary>
    public string ApiKeyEncrypted { get; private set; } = string.Empty;

    /// <summary>Hash (SHA-256) do token de webhook que a Tuilow gerou e registrou na conta do creator -- usado para autenticar webhooks recebidos sem precisar decriptar nada (ver AsaasWebhookController).</summary>
    public string WebhookTokenHash { get; private set; } = string.Empty;

    public CreatorAsaasAccountStatus Status { get; private set; } = CreatorAsaasAccountStatus.NotConnected;

    /// <summary>CPF/CNPJ informado pelo creator no momento da conexao (auditoria/suporte -- nao usado para nenhuma decisao de acesso).</summary>
    public string? CpfCnpj { get; private set; }

    public string? LegalName { get; private set; }

    /// <summary>
    /// Override de comissao especifico deste creator (0-100). Nulo = usa o percentual padrao da
    /// plataforma (PlatformFeeConfiguration). Precedencia: override do creator -&gt; padrao da
    /// plataforma (ver GetEffectiveCommissionPercentage nos handlers de Sales/Finance).
    /// </summary>
    public decimal? CommissionOverridePercentage { get; private set; }

    public bool IsEnabledForSelling { get; private set; } = true;

    public string? LastValidationError { get; private set; }
    public DateTime? LastValidatedAt { get; private set; }
    public DateTime? LastWebhookReceivedAt { get; private set; }

    private CreatorAsaasAccount() { }

    public static CreatorAsaasAccount CreateConnecting(Guid creatorId, string? cpfCnpj, string? legalName) => new()
    {
        CreatorId = creatorId,
        Status = CreatorAsaasAccountStatus.PendingValidation,
        CpfCnpj = cpfCnpj?.Trim(),
        LegalName = legalName?.Trim()
    };

    /// <summary>Chamado apos validar a API Key com sucesso contra a API da Asaas e registrar o webhook.</summary>
    public void MarkValidated(string asaasAccountId, string walletId, string apiKeyEncrypted, string webhookTokenHash)
    {
        AsaasAccountId = asaasAccountId;
        WalletId = walletId;
        ApiKeyEncrypted = apiKeyEncrypted;
        WebhookTokenHash = webhookTokenHash;
        Status = CreatorAsaasAccountStatus.Active;
        LastValidationError = null;
        LastValidatedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkValidationFailed(string error)
    {
        Status = CreatorAsaasAccountStatus.Rejected;
        LastValidationError = error;
        LastValidatedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkRestricted(string reason)
    {
        Status = CreatorAsaasAccountStatus.Restricted;
        LastValidationError = reason;
        Touch();
    }

    public void SetEnabledForSelling(bool enabled)
    {
        IsEnabledForSelling = enabled;
        Touch();
    }

    public void SetCommissionOverride(decimal? percentage)
    {
        if (percentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "O percentual de comissao deve estar entre 0 e 100.");
        CommissionOverridePercentage = percentage;
        Touch();
    }

    public void RecordWebhookReceived()
    {
        LastWebhookReceivedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Apto a vender de verdade: validado pela Asaas E nao desativado manualmente pelo admin/creator.</summary>
    public bool CanSell => Status == CreatorAsaasAccountStatus.Active && IsEnabledForSelling;
}
