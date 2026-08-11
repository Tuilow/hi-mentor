using Tuilow.Finance.Application.Commands.UploadCreatorOnboardingDocument;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Tests.Fakes;
using Xunit;

namespace Tuilow.Finance.Tests.Application;

/// <summary>
/// UploadCreatorOnboardingDocumentCommandHandler compartilha a mesma etapa de "re-sincronizar
/// documentos depois de um efeito colateral na Asaas" que causava DbUpdateConcurrencyException em
/// SyncCreatorOnboardingDocumentsCommandHandler (ver o racional completo lá) — mesma correção
/// aplicada aqui. Diferença testada especificamente: o upload em si (chamada que MUDA estado na
/// Asaas) nunca deve ser repetido numa nova tentativa por corrida — só a releitura/gravação local.
/// </summary>
public class UploadCreatorOnboardingDocumentCommandHandlerTests
{
    private static (UploadCreatorOnboardingDocumentCommandHandler Handler, InMemoryCreatorAsaasSubaccountRepository Repository, FakeAsaasSubaccountClient Client, FakeUnitOfWork Uow, FakeSecretProtector Protector) BuildHandler()
    {
        var repository = new InMemoryCreatorAsaasSubaccountRepository();
        var client = new FakeAsaasSubaccountClient();
        var uow = new FakeUnitOfWork();
        var protector = new FakeSecretProtector();
        var handler = new UploadCreatorOnboardingDocumentCommandHandler(repository, client, protector, uow);
        return (handler, repository, client, uow, protector);
    }

    private static async Task<Guid> SeedSubaccountWithDocumentAsync(
        InMemoryCreatorAsaasSubaccountRepository repository, FakeSecretProtector protector,
        string asaasDocumentId, string? onboardingUrl = null)
    {
        var creatorId = Guid.NewGuid();
        var subaccount = CreatorAsaasSubaccount.Start(creatorId);
        subaccount.StartCollectingData(
            "Maria Criadora", "52998224725", new DateOnly(1990, 1, 1), null,
            "maria@example.com", "11999999999", null, 5000m,
            "Rua das Flores", "100", null, "Centro", "01000000");
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", protector.Protect("plaintext-key"), "hash_1");
        subaccount.SyncDocuments([(asaasDocumentId, "IDENTIFICATION", "RG", null, Tuilow.Finance.Domain.Enums.OnboardingDocumentStatus.Pending, onboardingUrl)]);
        await repository.AddAsync(subaccount);
        return creatorId;
    }

    private static UploadCreatorOnboardingDocumentCommand Command(Guid creatorId, string asaasDocumentId) =>
        new(creatorId, asaasDocumentId, new MemoryStream([1, 2, 3]), "rg.png", "image/png");

    [Fact]
    public async Task Handle_DocumentNotFound_ReturnsFailure_AndNeverCallsAsaas()
    {
        var (handler, repository, client, _, protector) = BuildHandler();
        var creatorId = await SeedSubaccountWithDocumentAsync(repository, protector, "doc_1");

        var result = await handler.Handle(Command(creatorId, "doc_inexistente"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(0, client.GetPendingDocumentsCallCount);
    }

    [Fact]
    public async Task Handle_DocumentHasOnboardingUrl_RefusesUpload()
    {
        var (handler, repository, _, _, protector) = BuildHandler();
        var creatorId = await SeedSubaccountWithDocumentAsync(repository, protector, "doc_1", onboardingUrl: "https://asaas.example/onboarding/doc_1");

        var result = await handler.Handle(Command(creatorId, "doc_1"), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("link oficial", result.ErrorMessage);
    }

    [Fact]
    public async Task Handle_SuccessfulUpload_ResyncsAndUpdatesExistingDocument_WithoutTreatingItAsNew()
    {
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedSubaccountWithDocumentAsync(repository, protector, "doc_1");
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "AWAITING_APPROVAL", null)];

        var result = await handler.Handle(Command(creatorId, "doc_1"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, repository.AddDocumentCallCount); // doc_1 já existia -- é atualizado, não recriado
        Assert.Equal(1, uow.TrySaveChangesCallCount);
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(Tuilow.Finance.Domain.Enums.OnboardingDocumentStatus.AwaitingApproval, subaccount!.Documents.Single(d => d.AsaasDocumentId == "doc_1").Status);
    }

    [Fact]
    public async Task Handle_ResyncDiscoversNewRequiredDocument_RegistersItExplicitlyAsAdded()
    {
        // Caso raro mas possível: a Asaas passa a exigir um documento adicional logo após o
        // upload do primeiro. A mesma correção (AddDocumentAsync explícito) se aplica aqui.
        var (handler, repository, client, _, protector) = BuildHandler();
        var creatorId = await SeedSubaccountWithDocumentAsync(repository, protector, "doc_1");
        client.NextPendingDocuments =
        [
            new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "AWAITING_APPROVAL", null),
            new AsaasOnboardingDocumentInfo("doc_2", "PROOF_OF_ADDRESS", "Comprovante", null, "PENDING", null)
        ];

        var result = await handler.Handle(Command(creatorId, "doc_1"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, repository.AddDocumentCallCount); // só doc_2 é novo
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(2, subaccount!.Documents.Count);
    }

    [Fact]
    public async Task Handle_ConcurrentConflictDuringResync_RetriesResyncOnly_NeverRepeatsTheUpload()
    {
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedSubaccountWithDocumentAsync(repository, protector, "doc_1");
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "AWAITING_APPROVAL", null)];
        uow.SimulatedConflictsBeforeSuccess = 1;

        var result = await handler.Handle(Command(creatorId, "doc_1"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, uow.TrySaveChangesCallCount); // 1 conflito + 1 sucesso na releitura
        // O upload (efeito colateral que muda estado na Asaas) nunca deve ser refeito — só a
        // releitura via GetPendingDocumentsAsync é repetida.
        Assert.Equal(2, client.GetPendingDocumentsCallCount);
        Assert.Equal(1, client.UploadDocumentCallCount);
    }
}
