using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.UploadCreatorOnboardingDocument;

public sealed class UploadCreatorOnboardingDocumentCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow
) : IRequestHandler<UploadCreatorOnboardingDocumentCommand, UploadCreatorOnboardingDocumentResult>
{
    public async Task<UploadCreatorOnboardingDocumentResult> Handle(UploadCreatorOnboardingDocumentCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (subaccount is null || subaccount.ApiKeyEncrypted is null)
            return new UploadCreatorOnboardingDocumentResult(false, "Onboarding financeiro ainda não iniciado.");

        var document = subaccount.Documents.FirstOrDefault(d => d.AsaasDocumentId == request.AsaasDocumentId);
        if (document is null)
            return new UploadCreatorOnboardingDocumentResult(false, "Documento não encontrado.");

        if (!string.IsNullOrEmpty(document.OnboardingUrl))
            return new UploadCreatorOnboardingDocumentResult(false, "Este documento só pode ser enviado pelo link oficial da Asaas.");

        var apiKey = secretProtector.Unprotect(subaccount.ApiKeyEncrypted);
        var uploaded = await asaasSubaccountClient.UploadDocumentAsync(
            apiKey, request.AsaasDocumentId, request.FileStream, request.FileName, request.ContentType, ct);

        if (!uploaded)
            return new UploadCreatorOnboardingDocumentResult(false, "Não foi possível enviar o documento. Tente novamente.");

        // Re-sincroniza pra refletir o novo status (normalmente PENDING -> AWAITING_APPROVAL)
        // sem esperar o próximo webhook/poll.
        var refreshed = await asaasSubaccountClient.GetPendingDocumentsAsync(apiKey, ct);
        var mapped = refreshed.Select(d => (
            d.Id, d.Type, d.Title, d.Description, MapStatus(d.Status), d.OnboardingUrl)).ToList();
        subaccount.SyncDocuments(mapped);
        repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);

        return new UploadCreatorOnboardingDocumentResult(true, null);
    }

    private static OnboardingDocumentStatus MapStatus(string asaasStatus) => asaasStatus.ToUpperInvariant() switch
    {
        "AWAITING_APPROVAL" => OnboardingDocumentStatus.AwaitingApproval,
        "APPROVED" => OnboardingDocumentStatus.Approved,
        "REJECTED" => OnboardingDocumentStatus.Rejected,
        _ => OnboardingDocumentStatus.Pending
    };
}
