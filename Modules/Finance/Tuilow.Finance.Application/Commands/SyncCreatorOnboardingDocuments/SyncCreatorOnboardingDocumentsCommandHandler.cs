using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.SyncCreatorOnboardingDocuments;

public sealed class SyncCreatorOnboardingDocumentsCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow
) : IRequestHandler<SyncCreatorOnboardingDocumentsCommand, SyncCreatorOnboardingDocumentsResult>
{
    public async Task<SyncCreatorOnboardingDocumentsResult> Handle(SyncCreatorOnboardingDocumentsCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (subaccount is null || subaccount.AsaasAccountId is null)
            return new SyncCreatorOnboardingDocumentsResult(false, "NotStarted", "Onboarding financeiro ainda não iniciado.");

        var apiKey = secretProtector.Unprotect(subaccount.ApiKeyEncrypted!);
        var documents = await asaasSubaccountClient.GetPendingDocumentsAsync(apiKey, ct);

        var mapped = documents.Select(d => (
            d.Id, d.Type, d.Title, d.Description, MapStatus(d.Status), d.OnboardingUrl)).ToList();

        subaccount.SyncDocuments(mapped);
        repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);

        return new SyncCreatorOnboardingDocumentsResult(true, subaccount.Status.ToString(), null);
    }

    private static OnboardingDocumentStatus MapStatus(string asaasStatus) => asaasStatus.ToUpperInvariant() switch
    {
        "AWAITING_APPROVAL" => OnboardingDocumentStatus.AwaitingApproval,
        "APPROVED" => OnboardingDocumentStatus.Approved,
        "REJECTED" => OnboardingDocumentStatus.Rejected,
        _ => OnboardingDocumentStatus.Pending
    };
}
