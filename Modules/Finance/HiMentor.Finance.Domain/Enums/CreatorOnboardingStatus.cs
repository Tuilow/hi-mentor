namespace HiMentor.Finance.Domain.Enums;

/// <summary>
/// Estado da jornada de onboarding financeiro do criador via subconta Asaas (BaaS) criada pela
/// própria HiMentor — modelo que substitui o fluxo legado de "cole sua API Key" (ver
/// <see cref="HiMentor.Finance.Domain.Entities.CreatorAsaasAccount"/>, mantido só por
/// compatibilidade histórica). Nenhuma etapa aqui expõe API Key/Wallet ID/"subconta" ao criador —
/// esses são detalhes de infraestrutura tratados só no backend (ver
/// <see cref="HiMentor.Finance.Domain.Entities.CreatorAsaasSubaccount"/>).
/// </summary>
public enum CreatorOnboardingStatus
{
    /// <summary>Criador ainda não iniciou o onboarding financeiro.</summary>
    NotStarted = 0,

    /// <summary>Dados pessoais/empresariais preenchidos, ainda não enviados à Asaas.</summary>
    CollectingData = 1,

    /// <summary>
    /// A chamada de criação da subconta (POST /v3/accounts) está em andamento ou pode ter
    /// falhado sem confirmação — estado persistido ANTES da chamada à Asaas para que um timeout
    /// não deixe o sistema sem registro do que foi tentado (ver StartCreatorFinancialOnboardingCommandHandler).
    /// </summary>
    AccountCreationPending = 2,

    /// <summary>Subconta criada na Asaas (AsaasAccountId/WalletId/ApiKeyEncrypted já persistidos), documentos ainda não consultados.</summary>
    AccountCreated = 3,

    /// <summary>Existem documentos pendentes de envio pelo criador (ver CreatorAsaasOnboardingDocument).</summary>
    DocumentsPending = 4,

    /// <summary>Todos os documentos conhecidos foram enviados; aguardando análise/KYC da Asaas.</summary>
    UnderReview = 5,

    /// <summary>Aprovado pela Asaas — apto a vender (ver CanSell).</summary>
    Approved = 6,

    /// <summary>Rejeitado pela Asaas (documentação, análise cadastral etc.) — ver RejectionReason.</summary>
    Rejected = 7,

    /// <summary>Bloqueado manualmente por um admin da HiMentor (ex.: suspeita de fraude).</summary>
    Blocked = 8
}
