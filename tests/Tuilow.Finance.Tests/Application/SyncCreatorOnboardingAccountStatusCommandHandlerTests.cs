using Microsoft.Extensions.Logging.Abstractions;
using Tuilow.Finance.Application.Commands.SyncCreatorOnboardingAccountStatus;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Tests.Fakes;
using Xunit;

namespace Tuilow.Finance.Tests.Application;

/// <summary>
/// Regressão do incidente de produção: um criador foi aprovado de verdade na Asaas
/// (ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED), mas a tela de onboarding financeiro continuou
/// mostrando "Análise" indefinidamente -- GetMyFinancialOnboardingStatusQuery só lê o banco, e o
/// único caminho que atualizava o veredito geral era o webhook de status de conta, que ou nunca
/// foi registrado (falha silenciosa em StartCreatorFinancialOnboardingCommandHandler, só um log
/// crítico) ou nunca chegou por qualquer motivo de rede -- sem nenhuma forma de recuperação.
///
/// Este handler é a rede de segurança: consultado a cada carregamento da tela de status (ver
/// CreatorFinancialOnboardingController.GetStatus), reconsulta GET /myAccount/status direto na
/// Asaas (throttlado) e reaplica a mesma transição de estado que o webhook aplicaria.
/// </summary>
public class SyncCreatorOnboardingAccountStatusCommandHandlerTests
{
    private static (SyncCreatorOnboardingAccountStatusCommandHandler Handler, InMemoryCreatorAsaasSubaccountRepository Repository, FakeAsaasSubaccountClient Client, FakeUnitOfWork Uow, FakeSecretProtector Protector) BuildHandler()
    {
        var repository = new InMemoryCreatorAsaasSubaccountRepository();
        var client = new FakeAsaasSubaccountClient();
        var uow = new FakeUnitOfWork();
        var protector = new FakeSecretProtector();
        var handler = new SyncCreatorOnboardingAccountStatusCommandHandler(
            repository, client, protector, uow, NullLogger<SyncCreatorOnboardingAccountStatusCommandHandler>.Instance);
        return (handler, repository, client, uow, protector);
    }

    private static async Task<(Guid CreatorId, CreatorAsaasSubaccount Subaccount)> SeedUnderReviewSubaccountAsync(
        InMemoryCreatorAsaasSubaccountRepository repository, FakeSecretProtector protector)
    {
        var creatorId = Guid.NewGuid();
        var subaccount = CreatorAsaasSubaccount.Start(creatorId);
        subaccount.StartCollectingData(
            "Maria Criadora", "52998224725", new DateOnly(1990, 1, 1), null,
            "maria@example.com", "11999999999", null, 5000m,
            "Rua das Flores", "100", null, "Centro", "01000000");
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated("acc_1", "wallet_1", protector.Protect("plaintext-key"), "hash_1");
        subaccount.SyncDocuments([("doc_1", "IDENTIFICATION", "RG", null, OnboardingDocumentStatus.AwaitingApproval, null)]);
        await repository.AddAsync(subaccount);
        return (creatorId, subaccount);
    }

    [Fact]
    public async Task Handle_NoSubaccount_DoesNothing()
    {
        var (handler, _, client, uow, _) = BuildHandler();

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, client.GetAccountStatusCallCount);
        Assert.Equal(0, uow.TrySaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_SubaccountApproved_AndPaymentWebhookAlreadyRegistered_NeverCallsAsaas()
    {
        // Estado totalmente quiescente -- veredito geral já final E webhook de pagamento já
        // confirmado -- não há motivo para chamar a Asaas de novo em nenhuma das duas frentes.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, subaccount) = await SeedUnderReviewSubaccountAsync(repository, protector);
        subaccount.MarkApproved();
        subaccount.MarkPaymentWebhookRegistered();

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        Assert.Equal(0, client.GetAccountStatusCallCount);
        Assert.Equal(0, client.RegisterWebhookCallCount);
        Assert.Equal(0, client.RegisterPaymentWebhookCallCount);
    }

    [Fact]
    public async Task Handle_SubaccountApproved_ButPaymentWebhookNeverRegistered_RegistersItRetroactively()
    {
        // Regressão do bug de produção: uma subconta aprovada ANTES desta proteção existir nunca
        // teve o webhook de pagamento registrado -- compras no marketplace desse criador nunca
        // recebiam confirmação de volta da Asaas. Mesmo já Approved (não precisa mais reconsultar
        // o veredito geral), o handler ainda deve registrar o webhook de pagamento que falta.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, subaccount) = await SeedUnderReviewSubaccountAsync(repository, protector);
        // MarkApproved() aqui, NUNCA ApplyAccountStatusSync("APPROVED") -- esta última também seta
        // LastAccountStatusSyncedAt = DateTime.UtcNow como efeito colateral (ver CreatorAsaasSubaccount),
        // o que dispararia o throttle de 20s do próprio handler.Handle() chamado logo abaixo (tempo real
        // decorrido no teste é ~0ms) e faria o teste "passar" só porque nada rodou, não porque a
        // reafirmação retroativa funcionou. MarkApproved() é o método usado pelo caminho real de
        // aprovação (webhook ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED) e não toca o timestamp de
        // throttle -- reproduz fielmente "subconta aprovada por webhook antes desta proteção existir".
        subaccount.MarkApproved();
        var hashBefore = subaccount.WebhookTokenHash;

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        // Já Approved -- não precisa reconsultar o veredito geral de novo.
        Assert.Equal(0, client.GetAccountStatusCallCount);
        // Mas os dois webhooks são reafirmados com um token novo (rotacionado), já que o token
        // original em texto puro não existe mais.
        Assert.Equal(1, client.RegisterWebhookCallCount);
        Assert.Equal(1, client.RegisterPaymentWebhookCallCount);

        var updated = await repository.GetByCreatorIdAsync(creatorId);
        Assert.NotNull(updated!.PaymentWebhookRegisteredAt);
        Assert.NotEqual(hashBefore, updated.WebhookTokenHash);
    }

    [Fact]
    public async Task Handle_PaymentWebhookRegistrationFails_NeverRotatesToken_AndRetriesLater()
    {
        // Se o webhook de STATUS falha ao ser reafirmado com o token novo, não rotaciona nada --
        // rotacionar de qualquer forma deixaria o webhook de status (que até então funcionava)
        // autenticando com um token que o hash salvo não reconhece mais.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, subaccount) = await SeedUnderReviewSubaccountAsync(repository, protector);
        subaccount.MarkApproved();
        var hashBefore = subaccount.WebhookTokenHash;
        client.NextWebhookRegistrationShouldSucceed = false;

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        var updated = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(hashBefore, updated!.WebhookTokenHash); // nunca rotacionado
        Assert.Null(updated.PaymentWebhookRegisteredAt);
        Assert.Equal(0, client.RegisterPaymentWebhookCallCount); // nem tentou -- evita token dessincronizado
    }

    [Fact]
    public async Task Handle_PaymentWebhookLegFailsAfterStatusLegSucceeds_StillRotatesToken_ButLeavesPaymentWebhookUnmarked()
    {
        // O webhook de status já foi reafirmado com sucesso (Asaas já está usando o token novo) --
        // o hash salvo TEM que acompanhar isso mesmo que o webhook de pagamento falhe em seguida,
        // senão o webhook de status para de autenticar.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, subaccount) = await SeedUnderReviewSubaccountAsync(repository, protector);
        subaccount.MarkApproved();
        var hashBefore = subaccount.WebhookTokenHash;
        client.NextPaymentWebhookRegistrationShouldSucceed = false;

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        var updated = await repository.GetByCreatorIdAsync(creatorId);
        Assert.NotEqual(hashBefore, updated!.WebhookTokenHash); // rotacionado mesmo com a segunda perna falhando
        Assert.Null(updated.PaymentWebhookRegisteredAt); // mas não marcado -- tenta de novo no próximo poll
    }

    [Fact]
    public async Task Handle_SubaccountStillCollectingData_NeverCallsAsaas()
    {
        // Ainda não tem AsaasAccountId -- nada para consultar.
        var repository = new InMemoryCreatorAsaasSubaccountRepository();
        var client = new FakeAsaasSubaccountClient();
        var uow = new FakeUnitOfWork();
        var protector = new FakeSecretProtector();
        var handler = new SyncCreatorOnboardingAccountStatusCommandHandler(
            repository, client, protector, uow, NullLogger<SyncCreatorOnboardingAccountStatusCommandHandler>.Instance);

        var creatorId = Guid.NewGuid();
        var subaccount = CreatorAsaasSubaccount.Start(creatorId);
        subaccount.StartCollectingData(
            "Maria Criadora", "52998224725", new DateOnly(1990, 1, 1), null,
            "maria@example.com", "11999999999", null, 5000m,
            "Rua das Flores", "100", null, "Centro", "01000000");
        await repository.AddAsync(subaccount);

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        Assert.Equal(0, client.GetAccountStatusCallCount);
    }

    [Fact]
    public async Task Handle_AsaasReportsGeneralApproved_UnsticksTheCreator_MovesToApproved()
    {
        // Cenário exato do incidente: preso em UnderReview, já aprovado de verdade na Asaas.
        var (handler, repository, client, uow, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = new AsaasAccountStatusInfo("APPROVED", "APPROVED", "APPROVED", "APPROVED");

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount!.Status);
        Assert.True(subaccount.CanSell);
        Assert.Equal(1, uow.TrySaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_AsaasReportsGeneralRejected_MovesToRejected()
    {
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = new AsaasAccountStatusInfo("REJECTED", null, null, null);

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(CreatorOnboardingStatus.Rejected, subaccount!.Status);
    }

    [Fact]
    public async Task Handle_AsaasCallFails_NeverThrows_AndNeverChangesStatus()
    {
        // Best-effort: Asaas fora do ar não pode derrubar a tela de status do criador.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = null; // simula falha (já logada dentro do client real)

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(CreatorOnboardingStatus.UnderReview, subaccount!.Status);
    }

    [Fact]
    public async Task Handle_CalledTwiceInQuickSuccession_Throttles_SecondCallNeverHitsAsaas()
    {
        // A tela de status é repolled com frequência -- sem throttle, cada poll bateria na Asaas.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = new AsaasAccountStatusInfo("AWAITING_APPROVAL", null, null, null);

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);
        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        Assert.Equal(1, client.GetAccountStatusCallCount);
    }

    [Fact]
    public async Task Handle_ThrottleAlsoAppliesAfterAFailedAttempt()
    {
        // Sem isso, uma Asaas fora do ar faria cada poll tentar de novo sem nenhum limite.
        var (handler, repository, client, _, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = null;

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);
        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        Assert.Equal(1, client.GetAccountStatusCallCount);
    }

    [Fact]
    public async Task Handle_ConcurrentWriteConflict_IsSwallowed_NeverThrows()
    {
        var (handler, repository, client, uow, protector) = BuildHandler();
        var (creatorId, _) = await SeedUnderReviewSubaccountAsync(repository, protector);
        client.NextAccountStatus = new AsaasAccountStatusInfo("AWAITING_APPROVAL", null, null, null);
        uow.SimulatedConflictsBeforeSuccess = 10; // sempre "perde" -- best-effort não deve tentar de novo nem lançar

        await handler.Handle(new SyncCreatorOnboardingAccountStatusCommand(creatorId), CancellationToken.None);

        Assert.Equal(1, uow.TrySaveChangesCallCount); // uma única tentativa, sem retry (não é operação crítica)
    }
}
