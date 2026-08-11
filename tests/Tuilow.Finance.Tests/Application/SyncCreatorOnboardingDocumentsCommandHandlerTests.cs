using Tuilow.Finance.Application.Commands.SyncCreatorOnboardingDocuments;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Tests.Fakes;
using Xunit;

namespace Tuilow.Finance.Tests.Application;

/// <summary>
/// Regressão do bug de produção: SyncCreatorOnboardingDocumentsCommandHandler lançava
/// DbUpdateConcurrencyException ("esperava afetar 1 linha, afetou 0") ao gravar documentos novos
/// pela primeira vez, mesmo com a chamada à Asaas retornando 200. Causa raiz: o handler chamava
/// repository.Update(subaccount) sobre o agregado inteiro depois de SyncDocuments adicionar
/// documentos novos à coleção em memória — como CreatorAsaasOnboardingDocument.Id é um Guid
/// gerado no cliente (já preenchido na criação), o EF Core tratava esses documentos novos como
/// Modified em vez de Added, gerando um UPDATE para uma linha nunca inserida. A correção: cada
/// documento novo devolvido por SyncDocuments é registrado explicitamente via
/// repository.AddDocumentAsync (mesmo padrão de ICourseRepository.AddLessonAsync em Catalog);
/// repository.Update(subaccount) não é mais chamado neste handler.
///
/// Estes testes usam os fakes em memória (InMemoryCreatorAsaasSubaccountRepository/FakeUnitOfWork),
/// que NÃO reproduzem o rastreamento Added/Modified real do EF Core — por isso não teriam pegado
/// o bug original sozinhos. O que eles verificam é o CONTRATO da correção (o handler chama
/// AddDocumentAsync para cada documento novo, nunca Update no agregado) e o comportamento
/// observável de idempotência/concorrência, que são independentes do provider de banco.
/// </summary>
public class SyncCreatorOnboardingDocumentsCommandHandlerTests
{
    private static (SyncCreatorOnboardingDocumentsCommandHandler Handler, InMemoryCreatorAsaasSubaccountRepository Repository, FakeAsaasSubaccountClient Client, FakeUnitOfWork Uow, FakeSecretProtector Protector) BuildHandler()
    {
        var repository = new InMemoryCreatorAsaasSubaccountRepository();
        var client = new FakeAsaasSubaccountClient();
        var uow = new FakeUnitOfWork();
        var protector = new FakeSecretProtector();
        var handler = new SyncCreatorOnboardingDocumentsCommandHandler(repository, client, protector, uow);
        return (handler, repository, client, uow, protector);
    }

    private static async Task<Guid> SeedAccountCreatedSubaccountAsync(InMemoryCreatorAsaasSubaccountRepository repository, FakeSecretProtector protector)
    {
        var creatorId = Guid.NewGuid();
        var subaccount = CreatorAsaasSubaccount.Start(creatorId);
        subaccount.StartCollectingData(
            "Maria Criadora", "52998224725", new DateOnly(1990, 1, 1), null,
            "maria@example.com", "11999999999", null, 5000m,
            "Rua das Flores", "100", null, "Centro", "01000000");
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", protector.Protect("plaintext-key"), "hash_1");
        await repository.AddAsync(subaccount);
        return creatorId;
    }

    [Fact]
    public async Task Handle_NoSubaccount_ReturnsNotStarted()
    {
        var (handler, _, _, _, _) = BuildHandler();

        var result = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("NotStarted", result.Status);
    }

    [Fact]
    public async Task Handle_FirstSync_RegistersNewDocumentExplicitlyAsAdded()
    {
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments =
        [
            new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", "https://asaas.example/onboarding/doc_1")
        ];

        var result = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, repository.AddDocumentCallCount);
        Assert.Equal(1, uow.TrySaveChangesCallCount);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Single(subaccount!.Documents);
        Assert.Equal("DocumentsPending", subaccount.Status.ToString());
    }

    [Fact]
    public async Task Handle_MultipleNewDocumentsInOneSync_RegistersEachOneAsAdded()
    {
        var (handler, repository, client, _, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments =
        [
            new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null),
            new AsaasOnboardingDocumentInfo("doc_2", "IDENTIFICATION_SELFIE", "Selfie", null, "PENDING", "https://asaas.example/onboarding/doc_2"),
            new AsaasOnboardingDocumentInfo("doc_3", "PROOF_OF_ADDRESS", "Comprovante", null, "PENDING", null)
        ];

        var result = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, repository.AddDocumentCallCount);
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(3, subaccount!.Documents.Count);
    }

    [Fact]
    public async Task Handle_CalledTwiceInARowWithSameAsaasResponse_IsIdempotent_NeverDuplicates()
    {
        // Item pedido na correção: o comando deve poder ser executado várias vezes sem gerar
        // inconsistência — chamando de novo com a mesma leitura da Asaas, o documento já existente
        // é atualizado no lugar (SyncFrom), nunca recriado/duplicado.
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments =
        [
            new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null)
        ];

        var first = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);
        var second = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, repository.AddDocumentCallCount); // só a primeira chamada tinha um documento novo
        Assert.Equal(2, uow.TrySaveChangesCallCount); // as duas chamadas salvam (status/UpdatedAt), mas sem duplicar
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Single(subaccount!.Documents);
    }

    [Fact]
    public async Task Handle_ThirdCallWithUpdatedStatus_UpdatesExistingDocument_WithoutTreatingItAsNew()
    {
        var (handler, repository, client, _, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null)];
        await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "APPROVED", null)];
        var result = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, repository.AddDocumentCallCount); // nunca chamou AddDocumentAsync de novo para doc_1
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Single(subaccount!.Documents);
        Assert.Equal(Tuilow.Finance.Domain.Enums.OnboardingDocumentStatus.Approved, subaccount.Documents.Single().Status);
    }

    [Fact]
    public async Task Handle_ConcurrentConflictOnFirstAttempt_RetriesWithFreshReadAndSucceeds()
    {
        // Simula duas requisições simultâneas descobrindo o mesmo documento novo ao mesmo tempo
        // (ex.: duas abas abertas na tela de documentos) -- a perdedora dessa corrida esbarra na
        // restrição única do banco; TrySaveChangesAsync devolve false em vez de propagar a
        // exceção, e o handler deve reconsultar e tentar de novo automaticamente.
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null)];
        uow.SimulatedConflictsBeforeSuccess = 1;

        var result = await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, uow.TrySaveChangesCallCount); // 1 tentativa perdida + 1 com sucesso
        Assert.Equal(2, client.GetPendingDocumentsCallCount); // reconsultou a Asaas na nova tentativa
    }

    [Fact]
    public async Task Handle_ConflictsExhaustAllAttempts_ThrowsInsteadOfFailingSilently()
    {
        // Regra explícita: nunca engolir a exceção de concorrência silenciosamente. Se a corrida
        // persistir além do número de tentativas (cenário extremamente improvável em produção),
        // a falha deve ser propagada, não ignorada.
        var (handler, repository, client, uow, protector) = BuildHandler();
        var creatorId = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null)];
        uow.SimulatedConflictsBeforeSuccess = 10; // sempre perde a corrida

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorId), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ConcurrentCallsForDifferentCreators_DoNotInterfereWithEachOther()
    {
        var (handler, repository, client, _, protector) = BuildHandler();
        var creatorA = await SeedAccountCreatedSubaccountAsync(repository, protector);
        var creatorB = await SeedAccountCreatedSubaccountAsync(repository, protector);
        client.NextPendingDocuments = [new AsaasOnboardingDocumentInfo("doc_1", "IDENTIFICATION", "RG", null, "PENDING", null)];

        await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorA), CancellationToken.None);
        await handler.Handle(new SyncCreatorOnboardingDocumentsCommand(creatorB), CancellationToken.None);

        var subaccountA = await repository.GetByCreatorIdAsync(creatorA);
        var subaccountB = await repository.GetByCreatorIdAsync(creatorB);
        Assert.Single(subaccountA!.Documents);
        Assert.Single(subaccountB!.Documents);
    }
}
