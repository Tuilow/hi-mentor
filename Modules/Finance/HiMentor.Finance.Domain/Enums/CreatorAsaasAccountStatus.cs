namespace HiMentor.Finance.Domain.Enums;

/// <summary>
/// Estado da conexao do creator com sua PROPRIA conta Asaas externa (nao uma subconta criada
/// pela HiMentor -- ver comentario em CreatorAsaasAccount sobre por que esse modelo foi escolhido).
/// </summary>
public enum CreatorAsaasAccountStatus
{
    /// <summary>Nunca conectou nenhuma API Key.</summary>
    NotConnected = 0,

    /// <summary>
    /// API Key informada, mas a validacao contra a Asaas (GET /myAccount/status ou equivalente)
    /// ainda nao confirmou que a conta esta apta a receber cobrancas reais.
    /// </summary>
    PendingValidation = 1,

    /// <summary>Validada e apta -- pode receber cobrancas de venda de curso via split.</summary>
    Active = 2,

    /// <summary>
    /// A Asaas retornou que a conta tem pendencia (documentacao, analise cadastral, etc.) --
    /// nao pode vender ate resolver diretamente com a Asaas e reconectar.
    /// </summary>
    Restricted = 3,

    /// <summary>API Key invalida/revogada, ou a validacao falhou de forma nao recuperavel.</summary>
    Rejected = 4,

    /// <summary>Desativada manualmente pelo admin (ex.: suspeita de fraude, pedido do proprio creator).</summary>
    Disabled = 5
}
