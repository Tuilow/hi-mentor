using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Finance.Domain.Enums;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Onboarding financeiro do criador via subconta Asaas (BaaS) criada pela própria Tuilow —
/// modelo que SUBSTITUI o fluxo legado de "cole sua API Key" (ver
/// <see cref="CreatorAsaasAccount"/>, mantido intocado só para compatibilidade histórica: FK de
/// <c>CoursePurchase.CreatorAsaasAccountId</c> em compras antigas e auditoria admin).
///
/// Diferença fundamental para <see cref="CreatorAsaasAccount"/>: aqui é a TUILOW quem cria a
/// conta na Asaas (POST /v3/accounts, credencial da conta pai — "Asaas:ApiKey") em nome do
/// criador — o criador nunca vê API Key, Wallet ID ou o termo "subconta". A documentação oficial
/// da Asaas (verificada em docs.asaas.com) confirma que esse endpoint aceita tanto pessoa física
/// (CPF + BirthDate) quanto pessoa jurídica (CNPJ + CompanyType), ao contrário do que o
/// comentário de <see cref="CreatorAsaasAccount"/> presumia.
///
/// ApiKeyEncrypted é a API Key DA PRÓPRIA SUBCONTA (não a da Tuilow) — devolvida pela Asaas uma
/// única vez na criação (nunca mais recuperável, só regenerável por um fluxo manual no painel da
/// Asaas). Protegida via ISecretProtector, nunca sai de Infrastructure — Application/Api só
/// enxergam Status/Wallet/Documentos/flags (mesma disciplina de CreatorAsaasAccount).
/// </summary>
public sealed class CreatorAsaasSubaccount : AggregateRoot
{
    public Guid CreatorId { get; private set; }

    public CreatorOnboardingStatus Status { get; private set; } = CreatorOnboardingStatus.NotStarted;

    // ─── Dados coletados do criador (StartCollectingData) — só os que a Asaas exige em POST /v3/accounts ───
    public string LegalName { get; private set; } = string.Empty;
    public string CpfCnpj { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; } // obrigatório só para pessoa física
    public string? CompanyType { get; private set; } // obrigatório só para pessoa jurídica (MEI/LIMITED/INDIVIDUAL/ASSOCIATION)
    public string Email { get; private set; } = string.Empty;
    public string MobilePhone { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public decimal IncomeValue { get; private set; } // faturamento/renda mensal — exigido pela Asaas
    public string Address { get; private set; } = string.Empty;
    public string AddressNumber { get; private set; } = string.Empty;
    public string? AddressComplement { get; private set; }
    public string Province { get; private set; } = string.Empty; // bairro
    public string PostalCode { get; private set; } = string.Empty;

    // ─── Resultado da criação da subconta na Asaas ───
    public string? AsaasAccountId { get; private set; }
    public string? WalletId { get; private set; }
    public string? ApiKeyEncrypted { get; private set; }

    /// <summary>
    /// Hash (SHA-256) do token de webhook desta subconta — mesmo idioma de
    /// CreatorAsaasAccount.WebhookTokenHash. Um ÚNICO token/hash é compartilhado pelos DOIS
    /// webhooks registrados nesta subconta (status de conta e pagamento — ver
    /// RegisterAccountStatusWebhookAsync/RegisterPaymentWebhookAsync em IAsaasSubaccountClient);
    /// não há tokens separados por finalidade.
    /// </summary>
    public string? WebhookTokenHash { get; private set; }

    public string? RejectionReason { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? LastDocumentsSyncedAt { get; private set; }

    /// <summary>
    /// Instante da última consulta best-effort a GET /myAccount/status (ver
    /// SyncCreatorOnboardingAccountStatusCommandHandler) — usado só para throttle, nunca para
    /// decidir transição de estado por conta própria.
    /// </summary>
    public DateTime? LastAccountStatusSyncedAt { get; private set; }

    /// <summary>
    /// Instante em que o webhook de PAGAMENTO (marketplace de split, ver
    /// Sales.AsaasMarketplacePaymentService) foi registrado/confirmado com sucesso nesta subconta
    /// — null até isso acontecer. Subcontas criadas antes desta proteção existir (ver
    /// StartCreatorFinancialOnboardingCommandHandler) nunca tiveram esse webhook registrado — só
    /// o de status de conta — e ficam presas aqui até SyncCreatorOnboardingAccountStatusCommandHandler
    /// registrá-lo retroativamente (rotacionando o token, ver RotateWebhookToken).
    /// </summary>
    public DateTime? PaymentWebhookRegisteredAt { get; private set; }

    private readonly List<CreatorAsaasOnboardingDocument> _documents = [];
    public IReadOnlyCollection<CreatorAsaasOnboardingDocument> Documents => _documents.AsReadOnly();

    private CreatorAsaasSubaccount() { }

    public static CreatorAsaasSubaccount Start(Guid creatorId) => new()
    {
        CreatorId = creatorId,
        Status = CreatorOnboardingStatus.NotStarted
    };

    /// <summary>
    /// Passo 1 da jornada ("Seus dados") — grava os dados pessoais/empresariais informados pelo
    /// criador. Pode ser chamado de novo em NotStarted/CollectingData/Rejected (permite reenviar
    /// dados corrigidos após uma rejeição), mas NUNCA depois que a subconta já foi criada
    /// (AsaasAccountId preenchido) — nesse ponto os dados cadastrais só podem mudar diretamente
    /// com a Asaas.
    /// </summary>
    public void StartCollectingData(
        string legalName, string cpfCnpj, DateOnly? birthDate, string? companyType,
        string email, string mobilePhone, string? phone, decimal incomeValue,
        string address, string addressNumber, string? addressComplement, string province, string postalCode)
    {
        if (AsaasAccountId is not null)
            throw new InvalidOperationException("A subconta já foi criada na Asaas — não é possível alterar os dados cadastrais por aqui.");

        LegalName = legalName.Trim();
        CpfCnpj = cpfCnpj.Trim();
        BirthDate = birthDate;
        CompanyType = companyType;
        Email = email.Trim();
        MobilePhone = mobilePhone.Trim();
        Phone = phone?.Trim();
        IncomeValue = incomeValue;
        Address = address.Trim();
        AddressNumber = addressNumber.Trim();
        AddressComplement = addressComplement?.Trim();
        Province = province.Trim();
        PostalCode = postalCode.Trim();
        RejectionReason = null;
        Status = CreatorOnboardingStatus.CollectingData;
        Touch();
    }

    /// <summary>
    /// Persistido IMEDIATAMENTE ANTES de chamar POST /v3/accounts (ver
    /// StartCreatorFinancialOnboardingCommandHandler) — se a chamada à Asaas travar/der timeout
    /// depois deste ponto, o estado no banco já mostra "em andamento" em vez de silenciosamente
    /// nada, o que evita uma segunda tentativa duplicar a subconta (ver
    /// IAsaasSubaccountClient.CreateSubaccountAsync e nota de recuperação no relatório final).
    /// </summary>
    public void MarkAccountCreationPending()
    {
        if (Status is not (CreatorOnboardingStatus.CollectingData or CreatorOnboardingStatus.AccountCreationPending))
            throw new InvalidOperationException($"Não é possível iniciar a criação da subconta a partir do estado {Status}.");

        Status = CreatorOnboardingStatus.AccountCreationPending;
        Touch();
    }

    /// <summary>Falha na chamada de criação (rede, validação, timeout) — volta para CollectingData preservando os dados já digitados, para o criador corrigir e reenviar. RejectionReason é reaproveitado aqui como "último erro", não como rejeição de KYC (ver RejectionReason ficar limpo assim que StartCollectingData for chamado de novo).</summary>
    public void MarkAccountCreationFailed(string reason)
    {
        if (AsaasAccountId is not null)
            return; // nunca reverte se a subconta já existe de fato — evita reabrir CollectingData sobre uma conta já criada
        Status = CreatorOnboardingStatus.CollectingData;
        RejectionReason = reason;
        Touch();
    }

    /// <summary>Chamado uma única vez, logo após a Asaas responder 2xx a POST /v3/accounts — idempotente (ver AsaasAccountId != null como guarda no handler).</summary>
    public void MarkAccountCreated(string asaasAccountId, string walletId, string apiKeyEncrypted, string webhookTokenHash)
    {
        if (AsaasAccountId is not null)
            return; // idempotente — já criada, nunca sobrescreve (proteção contra chamada duplicada do handler)

        AsaasAccountId = asaasAccountId;
        WalletId = walletId;
        ApiKeyEncrypted = apiKeyEncrypted;
        WebhookTokenHash = webhookTokenHash;
        Status = CreatorOnboardingStatus.AccountCreated;
        Touch();
    }

    /// <summary>
    /// Substitui a lista de documentos pela leitura mais recente de GET /v3/myAccount/documents
    /// (reconciliação por AsaasDocumentId — atualiza os existentes, adiciona os novos; a Asaas
    /// não remove documentos da lista, só muda status). Avança o estado automaticamente:
    /// DocumentsPending se algum documento ainda está Pending, UnderReview se todos já foram
    /// enviados (AwaitingApproval/Approved/Rejected).
    ///
    /// Devolve os documentos recém-criados nesta chamada (e só eles) para que o handler os
    /// registre explicitamente como EntityState.Added no repositório — ver
    /// ICreatorAsaasSubaccountRepository.AddDocumentAsync. Isso é necessário porque
    /// CreatorAsaasOnboardingDocument.Id (Guid gerado no cliente, já preenchido no momento da
    /// criação) é indistinguível de "já existe" para o EF Core: se o handler apenas chamasse
    /// repository.Update(subaccount) sobre o agregado inteiro, o EF trataria o documento novo
    /// como Modified em vez de Added, gerando um UPDATE para uma linha nunca inserida — e
    /// DbUpdateConcurrencyException ("esperava afetar 1 linha, afetou 0"). Mesmo padrão já usado
    /// em ICourseRepository.AddLessonAsync/AddModuleAsync/AddFaqItemAsync (Catalog).
    /// </summary>
    public IReadOnlyCollection<CreatorAsaasOnboardingDocument> SyncDocuments(IReadOnlyCollection<(string AsaasDocumentId, string Type, string Title, string? Description, OnboardingDocumentStatus Status, string? OnboardingUrl)> incoming)
    {
        if (Status is CreatorOnboardingStatus.NotStarted or CreatorOnboardingStatus.CollectingData or CreatorOnboardingStatus.AccountCreationPending)
            throw new InvalidOperationException($"Não é possível sincronizar documentos a partir do estado {Status} — a subconta ainda não foi criada.");

        var newlyAdded = new List<CreatorAsaasOnboardingDocument>();

        foreach (var item in incoming)
        {
            var existing = _documents.FirstOrDefault(d => d.AsaasDocumentId == item.AsaasDocumentId);
            if (existing is not null)
            {
                existing.SyncFrom(item.Title, item.Description, item.Status, item.OnboardingUrl);
            }
            else
            {
                var document = CreatorAsaasOnboardingDocument.Create(
                    Id, item.AsaasDocumentId, item.Type, item.Title, item.Description, item.Status, item.OnboardingUrl);
                _documents.Add(document);
                newlyAdded.Add(document);
            }
        }

        LastDocumentsSyncedAt = DateTime.UtcNow;

        if (Status is CreatorOnboardingStatus.Approved or CreatorOnboardingStatus.Rejected or CreatorOnboardingStatus.Blocked)
        {
            Touch();
            return newlyAdded; // estado final/administrativo não regride por causa de uma releitura de documentos
        }

        Status = _documents.Any(d => d.Status == OnboardingDocumentStatus.Pending)
            ? CreatorOnboardingStatus.DocumentsPending
            : CreatorOnboardingStatus.UnderReview;
        Touch();
        return newlyAdded;
    }

    /// <summary>
    /// Rede de segurança para o veredito GERAL de aprovação, que hoje só chega por dois
    /// caminhos: o webhook ACCOUNT_STATUS_GENERAL_APPROVAL_* (caminho principal, ver
    /// ProcessAsaasAccountStatusWebhookCommandHandler) ou este refresh best-effort contra GET
    /// /myAccount/status (ver SyncCreatorOnboardingAccountStatusCommandHandler), usado quando o
    /// webhook não chega — seja porque o registro dele falhou silenciosamente na criação da
    /// subconta (ver StartCreatorFinancialOnboardingCommandHandler, log crítico), seja por
    /// qualquer falha de entrega de rede. Sempre registra o instante da tentativa (mesmo sem
    /// transição), para o handler poder aplicar throttle e não bater na Asaas a cada poll da tela
    /// de status.
    /// </summary>
    public void ApplyAccountStatusSync(string? generalStatus)
    {
        LastAccountStatusSyncedAt = DateTime.UtcNow;

        switch (generalStatus?.Trim().ToUpperInvariant())
        {
            case "APPROVED":
                MarkApproved();
                break;

            case "REJECTED":
                MarkRejected("Encontramos uma pendência na sua documentação ou nos seus dados cadastrais. Reveja as informações enviadas.");
                break;

            case "AWAITING_APPROVAL":
            case "PENDING":
                MarkUnderReview();
                break;

            default:
                // null/valor desconhecido -- não interpreta como transição; só registra a
                // tentativa (já feito acima) para o throttle funcionar mesmo quando a Asaas não
                // devolve nada de útil em "general".
                Touch();
                break;
        }
    }

    /// <summary>
    /// Rotaciona o hash do token de webhook desta subconta — necessário quando um SEGUNDO
    /// webhook precisa ser registrado depois da criação da subconta (ver
    /// SyncCreatorOnboardingAccountStatusCommandHandler) e o token original em texto puro não
    /// existe mais em lugar nenhum (só o hash é persistido, por design — ver MarkAccountCreated).
    /// Como os dois webhooks desta subconta (status de conta e pagamento) compartilham o MESMO
    /// token/hash, rotacionar aqui só é seguro se TODOS os webhooks ativos forem reafirmados na
    /// Asaas com o token novo NA MESMA operação — nunca rotacione sem já ter confirmado isso.
    /// </summary>
    public void RotateWebhookToken(string newWebhookTokenHash)
    {
        if (AsaasAccountId is null)
            throw new InvalidOperationException("Não é possível rotacionar o token de webhook antes da subconta ser criada.");

        WebhookTokenHash = newWebhookTokenHash;
        Touch();
    }

    /// <summary>Marca que o webhook de PAGAMENTO desta subconta foi registrado/confirmado com sucesso — ver PaymentWebhookRegisteredAt.</summary>
    public void MarkPaymentWebhookRegistered()
    {
        PaymentWebhookRegisteredAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Aplicado pelo webhook ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED (ou pelo refresh best-effort acima).</summary>
    public void MarkApproved()
    {
        if (Status == CreatorOnboardingStatus.Blocked) return; // bloqueio manual do admin tem precedência sobre evento da Asaas
        Status = CreatorOnboardingStatus.Approved;
        RejectionReason = null;
        ApprovedAt = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Aplicado por qualquer evento ACCOUNT_STATUS_*_REJECTED. Reason em linguagem simples — nunca o código bruto da Asaas (ver GetMyFinancialOnboardingStatusQueryHandler).</summary>
    public void MarkRejected(string reason)
    {
        if (Status == CreatorOnboardingStatus.Blocked) return;
        Status = CreatorOnboardingStatus.Rejected;
        RejectionReason = reason;
        Touch();
    }

    public void MarkUnderReview()
    {
        if (Status is CreatorOnboardingStatus.Approved or CreatorOnboardingStatus.Blocked) return;
        Status = CreatorOnboardingStatus.UnderReview;
        Touch();
    }

    /// <summary>Ação administrativa (suspeita de fraude, pedido do próprio criador) — tem precedência sobre qualquer evento futuro da Asaas até ser desbloqueado.</summary>
    public void Block(string reason)
    {
        Status = CreatorOnboardingStatus.Blocked;
        RejectionReason = reason;
        Touch();
    }

    /// <summary>Reverte um bloqueio manual — volta para o estado que os dados/documentos atuais indicariam (aprovado se já havia sido aprovado antes do bloqueio, senão em análise).</summary>
    public void Unblock(bool wasApprovedBeforeBlock)
    {
        Status = wasApprovedBeforeBlock ? CreatorOnboardingStatus.Approved : CreatorOnboardingStatus.UnderReview;
        RejectionReason = null;
        Touch();
    }

    /// <summary>Apto a vender de verdade: aprovado pela Asaas E não bloqueado manualmente.</summary>
    public bool CanSell => Status == CreatorOnboardingStatus.Approved;
}
