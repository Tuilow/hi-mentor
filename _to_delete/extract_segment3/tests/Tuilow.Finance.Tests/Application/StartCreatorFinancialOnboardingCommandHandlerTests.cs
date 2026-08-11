using Microsoft.Extensions.Logging.Abstractions;
using Tuilow.Finance.Application.Commands.StartCreatorFinancialOnboarding;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Tests.Fakes;
using Xunit;

namespace Tuilow.Finance.Tests.Application;

public class StartCreatorFinancialOnboardingCommandHandlerTests
{
    private static StartCreatorFinancialOnboardingCommand ValidCommand(Guid creatorId) => new(
        CreatorId: creatorId,
        LegalName: "Maria Criadora",
        CpfCnpj: "52998224725",
        BirthDate: new DateOnly(1990, 1, 1),
        CompanyType: null,
        Email: "maria@example.com",
        MobilePhone: "11999999999",
        Phone: null,
        IncomeValue: 5000m,
        Address: "Rua das Flores",
        AddressNumber: "100",
        AddressComplement: null,
        Province: "Centro",
        PostalCode: "01000000");

    private static (StartCreatorFinancialOnboardingCommandHandler Handler, InMemoryCreatorAsaasSubaccountRepository Repository, FakeAsaasSubaccountClient Client, FakeUnitOfWork Uow) BuildHandler()
    {
        var repository = new InMemoryCreatorAsaasSubaccountRepository();
        var client = new FakeAsaasSubaccountClient();
        var uow = new FakeUnitOfWork();
        var handler = new StartCreatorFinancialOnboardingCommandHandler(
            repository, client, new FakeSecretProtector(), uow,
            NullLogger<StartCreatorFinancialOnboardingCommandHandler>.Instance);
        return (handler, repository, client, uow);
    }

    [Fact]
    public async Task Handle_FirstCall_CreatesSubaccount_AndPersistsApiKeyEncrypted()
    {
        var (handler, repository, client, _) = BuildHandler();
        var creatorId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, client.CreateSubaccountCallCount);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.NotNull(subaccount);
        Assert.Equal(CreatorOnboardingStatus.AccountCreated, subaccount!.Status);
        Assert.Equal("acc_fake_123", subaccount.AsaasAccountId);
        // A API Key nunca deve ficar em texto puro no agregado -- ver FakeSecretProtector.
        Assert.NotEqual(client.NextApiKey, subaccount.ApiKeyEncrypted);
        Assert.NotNull(subaccount.WebhookTokenHash);
    }

    [Fact]
    public async Task Handle_FirstCall_RegistersBothWebhooks_AndMarksPaymentWebhookRegistered()
    {
        // Regressão do bug de produção: compra marketplace nunca recebia confirmação de volta da
        // Asaas porque só o webhook de status de conta era registrado na criação da subconta,
        // nunca o de pagamento. Ver SyncCreatorOnboardingAccountStatusCommandHandler para o
        // registro retroativo em subcontas já existentes antes desta correção.
        var (handler, repository, client, _) = BuildHandler();
        var creatorId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, client.RegisterWebhookCallCount);
        Assert.Equal(1, client.RegisterPaymentWebhookCallCount);

        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.NotNull(subaccount!.PaymentWebhookRegisteredAt);
    }

    [Fact]
    public async Task Handle_WhenPaymentWebhookRegistrationFails_StillSucceeds_ButLeavesItUnmarked()
    {
        var (handler, repository, client, _) = BuildHandler();
        client.NextPaymentWebhookRegistrationShouldSucceed = false;
        var creatorId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Null(subaccount!.PaymentWebhookRegisteredAt);
    }

    [Fact]
    public async Task Handle_CalledTwiceForSameCreator_NeverCreatesTwoSubaccounts()
    {
        // Item 15 do briefing: clique duplo / refresh / retry não pode duplicar a subconta na Asaas.
        var (handler, repository, client, _) = BuildHandler();
        var creatorId = Guid.NewGuid();

        var first = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);
        var second = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, client.CreateSubaccountCallCount);

        var all = await repository.GetAllAsync(0, 100);
        Assert.Single(all);
    }

    [Fact]
    public async Task Handle_WhenAsaasCallFails_RevertsToCollectingData_AndNeverPersistsPartialAccountId()
    {
        var (handler, repository, client, _) = BuildHandler();
        client.NextCreateShouldSucceed = false;
        client.NextErrorMessage = "CPF inválido para a Asaas.";
        var creatorId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.False(result.Success);
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.NotNull(subaccount);
        Assert.Equal(CreatorOnboardingStatus.CollectingData, subaccount!.Status);
        Assert.Null(subaccount.AsaasAccountId);
        Assert.Equal("CPF inválido para a Asaas.", subaccount.RejectionReason);
    }

    [Fact]
    public async Task Handle_AfterFailure_RetrySucceeds_AndStillCreatesOnlyOneSubaccount()
    {
        // Simula timeout/erro seguido de nova tentativa do usuário -- recuperação de falha (item 16).
        var (handler, repository, client, _) = BuildHandler();
        client.NextCreateShouldSucceed = false;
        var creatorId = Guid.NewGuid();

        await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        client.NextCreateShouldSucceed = true;
        var retryResult = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(retryResult.Success);
        Assert.Equal(2, client.CreateSubaccountCallCount); // uma falha + uma que teve sucesso
        var all = await repository.GetAllAsync(0, 100);
        Assert.Single(all); // continua sendo uma única subconta lógica para o creator
    }

    [Fact]
    public async Task Handle_WhenWebhookRegistrationFails_StillReturnsSuccess_BecauseAccountAlreadyExists()
    {
        // A subconta já foi criada de verdade na Asaas nesse ponto -- não há como "desfazer", então
        // o onboarding não deve travar o creator por causa de uma falha só no registro do webhook
        // (ver LogCritical no handler real; aqui só garantimos o comportamento observável).
        var (handler, repository, client, _) = BuildHandler();
        client.NextWebhookRegistrationShouldSucceed = false;
        var creatorId = Guid.NewGuid();

        var result = await handler.Handle(ValidCommand(creatorId), CancellationToken.None);

        Assert.True(result.Success);
        var subaccount = await repository.GetByCreatorIdAsync(creatorId);
        Assert.Equal(CreatorOnboardingStatus.AccountCreated, subaccount!.Status);
    }
}
