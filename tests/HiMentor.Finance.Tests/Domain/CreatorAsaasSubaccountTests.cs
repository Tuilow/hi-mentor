using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Enums;
using Xunit;

namespace HiMentor.Finance.Tests.Domain;

public class CreatorAsaasSubaccountTests
{
    private static CreatorAsaasSubaccount NewStarted() => CreatorAsaasSubaccount.Start(Guid.NewGuid());

    private static void FillCollectingData(CreatorAsaasSubaccount subaccount) =>
        subaccount.StartCollectingData(
            legalName: "Maria Criadora", cpfCnpj: "52998224725", birthDate: new DateOnly(1990, 1, 1),
            companyType: null, email: "maria@example.com", mobilePhone: "11999999999", phone: null,
            incomeValue: 5000m, address: "Rua das Flores", addressNumber: "100", addressComplement: null,
            province: "Centro", postalCode: "01000000");

    [Fact]
    public void Start_BeginsAsNotStarted_AndCannotSell()
    {
        var subaccount = NewStarted();

        Assert.Equal(CreatorOnboardingStatus.NotStarted, subaccount.Status);
        Assert.False(subaccount.CanSell);
    }

    [Fact]
    public void StartCollectingData_MovesToCollectingData()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);

        Assert.Equal(CreatorOnboardingStatus.CollectingData, subaccount.Status);
        Assert.Equal("Maria Criadora", subaccount.LegalName);
    }

    [Fact]
    public void StartCollectingData_AfterAccountCreated_Throws()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", "protected-key", "hash");

        Assert.Throws<InvalidOperationException>(() => FillCollectingData(subaccount));
    }

    [Fact]
    public void MarkAccountCreationPending_FromNotStarted_Throws()
    {
        var subaccount = NewStarted();

        Assert.Throws<InvalidOperationException>(subaccount.MarkAccountCreationPending);
    }

    [Fact]
    public void MarkAccountCreated_IsIdempotent_SecondCallIsNoOp()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);
        subaccount.MarkAccountCreationPending();

        subaccount.MarkAccountCreated("acc_1", "wallet_1", "protected-key-1", "hash-1");
        // Chamada duplicada (ex.: reprocessamento) com dados diferentes -- nunca deve sobrescrever
        // a subconta já criada de verdade (ver comentário no próprio agregado).
        subaccount.MarkAccountCreated("acc_2", "wallet_2", "protected-key-2", "hash-2");

        Assert.Equal("acc_1", subaccount.AsaasAccountId);
        Assert.Equal("wallet_1", subaccount.WalletId);
        Assert.Equal("protected-key-1", subaccount.ApiKeyEncrypted);
    }

    [Fact]
    public void MarkAccountCreationFailed_AfterAccountAlreadyCreated_IsNoOp()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", "protected-key", "hash");

        subaccount.MarkAccountCreationFailed("timeout tardio, resposta perdida");

        // Não pode "desfazer" uma conta que já existe de verdade na Asaas.
        Assert.Equal(CreatorOnboardingStatus.AccountCreated, subaccount.Status);
        Assert.Equal("acc_1", subaccount.AsaasAccountId);
    }

    [Fact]
    public void MarkAccountCreationFailed_BeforeAccountCreated_RevertsToCollectingData()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);
        subaccount.MarkAccountCreationPending();

        subaccount.MarkAccountCreationFailed("Falha simulada na criação da subconta.");

        Assert.Equal(CreatorOnboardingStatus.CollectingData, subaccount.Status);
        Assert.Null(subaccount.AsaasAccountId);
        Assert.Equal("Falha simulada na criação da subconta.", subaccount.RejectionReason);
    }

    [Fact]
    public void SyncDocuments_BeforeAccountCreated_Throws()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);

        Assert.Throws<InvalidOperationException>(() =>
            subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, null)]));
    }

    [Fact]
    public void SyncDocuments_WithPendingDocument_MovesToDocumentsPending()
    {
        var subaccount = CreateAccountCreatedSubaccount();

        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, "https://asaas.example/onboarding/doc_1")]);

        Assert.Equal(CreatorOnboardingStatus.DocumentsPending, subaccount.Status);
        Assert.Single(subaccount.Documents);
    }

    [Fact]
    public void SyncDocuments_WithAllDocumentsSubmitted_MovesToUnderReview()
    {
        var subaccount = CreateAccountCreatedSubaccount();

        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.AwaitingApproval, null)]);

        Assert.Equal(CreatorOnboardingStatus.UnderReview, subaccount.Status);
    }

    [Fact]
    public void SyncDocuments_NewDocument_IsReturnedInResult_ForExplicitAddedTracking()
    {
        // Regressão do bug de persistência: o handler precisa saber EXATAMENTE quais documentos
        // são novos (para registrá-los como EntityState.Added explicitamente) — nunca inferir
        // isso a partir da coleção inteira, já que documentos já existentes também estão lá.
        var subaccount = CreateAccountCreatedSubaccount();

        var newDocuments = subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, null)]);

        Assert.Single(newDocuments);
        Assert.Equal("doc_1", newDocuments.Single().AsaasDocumentId);
        Assert.Single(subaccount.Documents);
    }

    [Fact]
    public void SyncDocuments_CalledTwiceWithSameDocument_SecondCallReturnsNoNewDocuments_AndDoesNotDuplicate()
    {
        // Idempotência: rodar o comando de sincronização várias vezes com a mesma leitura da
        // Asaas nunca deve duplicar documentos nem devolver o mesmo documento como "novo" de novo.
        var subaccount = CreateAccountCreatedSubaccount();
        var incoming = new (string, string, string, string?, OnboardingDocumentStatus, string?)[]
        {
            ("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, null)
        };

        var firstCallNewDocuments = subaccount.SyncDocuments(incoming);
        var secondCallNewDocuments = subaccount.SyncDocuments(incoming);

        Assert.Single(firstCallNewDocuments);
        Assert.Empty(secondCallNewDocuments);
        Assert.Single(subaccount.Documents); // nunca duplica
    }

    [Fact]
    public void SyncDocuments_ExistingDocumentWithUpdatedStatus_IsUpdatedInPlace_NotReturnedAsNew()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, null)]);

        var newDocuments = subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Approved, null)]);

        Assert.Empty(newDocuments);
        Assert.Single(subaccount.Documents);
        Assert.Equal(OnboardingDocumentStatus.Approved, subaccount.Documents.Single().Status);
    }

    [Fact]
    public void SyncDocuments_AfterApproved_DoesNotRegressStatus()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Approved, null)]);
        subaccount.MarkApproved();

        // Releitura periódica de documentos não deve derrubar um creator já aprovado, mesmo que
        // volte a aparecer um documento pendente novo (ex.: renovação solicitada pela Asaas).
        subaccount.SyncDocuments([("doc_2", "PROOF_OF_ADDRESS", "Comprovante", null, OnboardingDocumentStatus.Pending, null)]);

        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
        Assert.True(subaccount.CanSell);
    }

    [Fact]
    public void MarkApproved_SetsCanSellTrue_AndClearsRejectionReason()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.MarkRejected("pendência anterior");

        subaccount.MarkApproved();

        Assert.True(subaccount.CanSell);
        Assert.Null(subaccount.RejectionReason);
        Assert.NotNull(subaccount.ApprovedAt);
    }

    [Fact]
    public void MarkRejected_SetsCanSellFalse()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.MarkApproved();

        subaccount.MarkRejected("Encontramos uma pendência na sua documentação.");

        Assert.False(subaccount.CanSell);
        Assert.Equal(CreatorOnboardingStatus.Rejected, subaccount.Status);
    }

    [Fact]
    public void Block_TakesPrecedenceOverApprovedOrRejectedEvents()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.MarkApproved();

        subaccount.Block("Suspeita de fraude reportada.");
        // Eventos de webhook chegando depois do bloqueio administrativo não devem reverter o bloqueio
        // (ver comentário em MarkApproved/MarkRejected sobre precedência do bloqueio manual).
        subaccount.MarkApproved();
        subaccount.MarkRejected("qualquer motivo");
        subaccount.MarkUnderReview();

        Assert.Equal(CreatorOnboardingStatus.Blocked, subaccount.Status);
        Assert.False(subaccount.CanSell);
    }

    [Fact]
    public void Unblock_RestoresApproved_WhenWasApprovedBeforeBlock()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.MarkApproved();
        subaccount.Block("revisão manual");

        subaccount.Unblock(wasApprovedBeforeBlock: true);

        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
        Assert.True(subaccount.CanSell);
    }

    [Fact]
    public void Unblock_RestoresUnderReview_WhenWasNotApprovedBeforeBlock()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.Block("revisão manual");

        subaccount.Unblock(wasApprovedBeforeBlock: false);

        Assert.Equal(CreatorOnboardingStatus.UnderReview, subaccount.Status);
        Assert.False(subaccount.CanSell);
    }

    // ─── ApplyAccountStatusSync -- rede de segurança para o veredito geral (ver
    // SyncCreatorOnboardingAccountStatusCommandHandler), usada quando o webhook ACCOUNT_STATUS_*
    // não chega. ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyAccountStatusSync_GeneralApproved_MarksApproved_AndAllowsSelling()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.AwaitingApproval, null)]);

        subaccount.ApplyAccountStatusSync("APPROVED");

        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
        Assert.True(subaccount.CanSell);
        Assert.NotNull(subaccount.ApprovedAt);
        Assert.NotNull(subaccount.LastAccountStatusSyncedAt);
    }

    [Fact]
    public void ApplyAccountStatusSync_GeneralRejected_MarksRejected_WithGenericReason()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.AwaitingApproval, null)]);

        subaccount.ApplyAccountStatusSync("REJECTED");

        Assert.Equal(CreatorOnboardingStatus.Rejected, subaccount.Status);
        Assert.False(subaccount.CanSell);
        Assert.NotNull(subaccount.RejectionReason);
    }

    [Theory]
    [InlineData("AWAITING_APPROVAL")]
    [InlineData("PENDING")]
    public void ApplyAccountStatusSync_GeneralStillPending_KeepsUnderReview(string generalStatus)
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.AwaitingApproval, null)]);

        subaccount.ApplyAccountStatusSync(generalStatus);

        Assert.Equal(CreatorOnboardingStatus.UnderReview, subaccount.Status);
    }

    [Fact]
    public void ApplyAccountStatusSync_NullOrUnknownStatus_RecordsAttempt_ButNeverChangesStatus()
    {
        // Caso da falha de rede/Asaas (ver handler): a tentativa é registrada (para o throttle
        // funcionar), mas nenhuma transição de estado acontece por falta de dado confiável.
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.Pending, null)]);
        var statusBefore = subaccount.Status;

        subaccount.ApplyAccountStatusSync(null);

        Assert.Equal(statusBefore, subaccount.Status);
        Assert.NotNull(subaccount.LastAccountStatusSyncedAt);
    }

    [Fact]
    public void ApplyAccountStatusSync_AfterAlreadyBlocked_NeverOverridesBlock()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        subaccount.Block("suspeita de fraude");

        subaccount.ApplyAccountStatusSync("APPROVED");

        // Bloqueio manual do admin tem precedência sobre qualquer veredito da Asaas -- mesma
        // disciplina já aplicada pelo caminho do webhook (ver MarkApproved).
        Assert.Equal(CreatorOnboardingStatus.Blocked, subaccount.Status);
    }

    // ─── RotateWebhookToken / MarkPaymentWebhookRegistered -- registro retroativo do webhook de
    // pagamento (ver SyncCreatorOnboardingAccountStatusCommandHandler), motivado pelo bug de
    // produção onde uma compra marketplace nunca recebia confirmação de volta da Asaas. ────────

    [Fact]
    public void RotateWebhookToken_UpdatesHash()
    {
        var subaccount = CreateAccountCreatedSubaccount();

        subaccount.RotateWebhookToken("novo-hash-abc");

        Assert.Equal("novo-hash-abc", subaccount.WebhookTokenHash);
    }

    [Fact]
    public void RotateWebhookToken_BeforeAccountCreated_Throws()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);

        Assert.Throws<InvalidOperationException>(() => subaccount.RotateWebhookToken("novo-hash"));
    }

    [Fact]
    public void MarkPaymentWebhookRegistered_SetsTimestamp()
    {
        var subaccount = CreateAccountCreatedSubaccount();
        Assert.Null(subaccount.PaymentWebhookRegisteredAt);

        subaccount.MarkPaymentWebhookRegistered();

        Assert.NotNull(subaccount.PaymentWebhookRegisteredAt);
    }

    private static CreatorAsaasSubaccount CreateAccountCreatedSubaccount()
    {
        var subaccount = NewStarted();
        FillCollectingData(subaccount);
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", "protected-key", "hash");
        return subaccount;
    }
}
