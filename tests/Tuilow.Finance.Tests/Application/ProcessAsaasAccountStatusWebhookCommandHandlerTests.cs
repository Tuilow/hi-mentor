using Microsoft.Extensions.Logging.Abstractions;
using Tuilow.Finance.Application.Commands.ProcessAsaasAccountStatusWebhook;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Tests.Fakes;
using Xunit;

namespace Tuilow.Finance.Tests.Application;

public class ProcessAsaasAccountStatusWebhookCommandHandlerTests
{
    private static (ProcessAsaasAccountStatusWebhookCommandHandler Handler, InMemoryCreatorAsaasSubaccountRepository SubaccountRepository, InMemoryProcessedAsaasAccountEventRepository EventRepository) BuildHandler()
    {
        var subaccountRepository = new InMemoryCreatorAsaasSubaccountRepository();
        var eventRepository = new InMemoryProcessedAsaasAccountEventRepository();
        var handler = new ProcessAsaasAccountStatusWebhookCommandHandler(
            subaccountRepository, eventRepository, new FakeUnitOfWork(),
            NullLogger<ProcessAsaasAccountStatusWebhookCommandHandler>.Instance);
        return (handler, subaccountRepository, eventRepository);
    }

    private static async Task<CreatorAsaasSubaccount> SeedAccountCreatedSubaccountAsync(InMemoryCreatorAsaasSubaccountRepository repository, string asaasAccountId = "acc_1")
    {
        var subaccount = CreatorAsaasSubaccount.Start(Guid.NewGuid());
        subaccount.StartCollectingData(
            "Maria Criadora", "52998224725", new DateOnly(1990, 1, 1), null,
            "maria@example.com", "11999999999", null, 5000m,
            "Rua das Flores", "100", null, "Centro", "01000000");
        subaccount.MarkAccountCreationPending();
        subaccount.MarkAccountCreated(asaasAccountId, "wallet_1", "protected-key", "hash");
        await repository.AddAsync(subaccount);
        return subaccount;
    }

    [Fact]
    public async Task Handle_ApprovedEvent_MarksSubaccountApproved()
    {
        var (handler, subaccountRepository, _) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_1", "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
        Assert.True(subaccount.CanSell);
    }

    [Fact]
    public async Task Handle_RejectedEvent_UsesGenericMessage_NeverExposesRawAsaasEventName()
    {
        var (handler, subaccountRepository, _) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_1", "ACCOUNT_STATUS_DOCUMENT_REJECTED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.Equal(CreatorOnboardingStatus.Rejected, subaccount.Status);
        Assert.NotNull(subaccount.RejectionReason);
        Assert.DoesNotContain("ACCOUNT_STATUS", subaccount.RejectionReason);
        Assert.DoesNotContain("DOCUMENT_REJECTED", subaccount.RejectionReason);
    }

    [Fact]
    public async Task Handle_DuplicateEventId_IsNoOp_EvenWithDifferentPayload()
    {
        // Entrega "at-least-once" da Asaas -- o mesmo EventId pode chegar mais de uma vez (item 17
        // do briefing: idempotência de webhook).
        var (handler, subaccountRepository, eventRepository) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_dup", "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        // Reenvio com o MESMO EventId mas payload de evento diferente -- não deve ser reaplicado.
        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_dup", "ACCOUNT_STATUS_GENERAL_APPROVAL_REJECTED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
        Assert.Single(eventRepository.Items);
    }

    [Fact]
    public async Task Handle_UnknownAsaasAccountId_IsIgnoredSafely_AndStillMarkedProcessed()
    {
        var (handler, _, eventRepository) = BuildHandler();

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_1", "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", new AsaasAccountRef("acc_desconhecida"))), CancellationToken.None);

        // Não deve lançar exceção nem travar a fila de webhooks da Asaas; fica registrado como
        // processado para não ficar tentando reprocessar um evento órfão para sempre.
        Assert.Single(eventRepository.Items);
    }

    [Fact]
    public async Task Handle_UnmappedEventType_DoesNotChangeState_ButIsMarkedProcessed()
    {
        var (handler, subaccountRepository, eventRepository) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);
        var statusBefore = subaccount.Status;

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_1", "ACCOUNT_STATUS_COMMERCIAL_INFO_EXPIRING_SOON", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.Equal(statusBefore, subaccount.Status);
        Assert.Single(eventRepository.Items);
    }

    [Fact]
    public async Task Handle_OutOfOrderCategoryRejectedAfterApproved_DoesNotRevertApproval()
    {
        // Cenário do item 17 do briefing: eventos podem chegar fora de ordem. Um REJECTED de uma
        // categoria (ex.: documentação) reentregue com atraso, depois de um GENERAL_APPROVAL já ter
        // aprovado o creator, não deve derrubar a aprovação -- só um novo GENERAL_APPROVAL_REJECTED
        // (o veredito geral da própria Asaas) pode fazer isso.
        var (handler, subaccountRepository, _) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_approved", "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.True(subaccount.CanSell);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_old_rejected", "ACCOUNT_STATUS_DOCUMENT_REJECTED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.True(subaccount.CanSell);
        Assert.Equal(CreatorOnboardingStatus.Approved, subaccount.Status);
    }

    [Fact]
    public async Task Handle_GeneralApprovalRejectedAfterApproved_StillRevertsApproval()
    {
        // Diferente do evento de categoria acima: GENERAL_APPROVAL_REJECTED é o veredito geral da
        // própria Asaas revendo uma aprovação anterior -- esse sim deve sempre ser aplicado.
        var (handler, subaccountRepository, _) = BuildHandler();
        var subaccount = await SeedAccountCreatedSubaccountAsync(subaccountRepository);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_approved", "ACCOUNT_STATUS_GENERAL_APPROVAL_APPROVED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        await handler.Handle(new ProcessAsaasAccountStatusWebhookCommand(
            new AsaasAccountStatusPayload("evt_general_rejected", "ACCOUNT_STATUS_GENERAL_APPROVAL_REJECTED", new AsaasAccountRef(subaccount.AsaasAccountId!))), CancellationToken.None);

        Assert.False(subaccount.CanSell);
        Assert.Equal(CreatorOnboardingStatus.Rejected, subaccount.Status);
    }
}
