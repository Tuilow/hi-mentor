using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.UploadCreatorOnboardingDocument;

/// <summary>
/// Ver documentação de SyncCreatorOnboardingDocumentsCommandHandler para o racional completo do
/// bug de persistência corrigido aqui (EF Core tratando documentos novos como Modified em vez de
/// Added) e da estratégia de nova tentativa em caso de corrida com uma sincronização concorrente.
/// Diferença importante em relação àquele handler: o upload em si (chamada de rede que MUDA
/// estado na Asaas) roda uma única vez, fora do laço de nova tentativa — só a releitura/gravação
/// local (idempotente, sem efeito colateral externo) é repetida em caso de corrida.
/// </summary>
public sealed class UploadCreatorOnboardingDocumentCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow
) : IRequestHandler<UploadCreatorOnboardingDocumentCommand, UploadCreatorOnboardingDocumentResult>
{
    private const int MaxAttempts = 3;

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

        // Re-sincroniza pra refletir o novo status (normalmente PENDING -> AWAITING_APPROVAL) sem
        // esperar o próximo webhook/poll. Só esta parte (releitura + gravação local, sem nenhuma
        // chamada que mude estado na Asaas) é repetida em caso de corrida com um GetDocuments
        // simultâneo — o upload acima já aconteceu e não deve ser refeito.
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var current = attempt == 1 ? subaccount : await repository.GetByCreatorIdAsync(request.CreatorId, ct);
            if (current is null)
                return new UploadCreatorOnboardingDocumentResult(false, "Onboarding financeiro ainda não iniciado.");

            var refreshed = await asaasSubaccountClient.GetPendingDocumentsAsync(apiKey, ct);
            var mapped = refreshed.Select(d => (
                d.Id, d.Type, d.Title, d.Description, MapStatus(d.Status), d.OnboardingUrl)).ToList();

            var newDocuments = current.SyncDocuments(mapped);
            foreach (var newDocument in newDocuments)
                await repository.AddDocumentAsync(newDocument, ct);

            if (await uow.TrySaveChangesAsync(ct))
                return new UploadCreatorOnboardingDocumentResult(true, null);
        }

        throw new InvalidOperationException(
            "Não foi possível sincronizar os documentos do onboarding financeiro após múltiplas tentativas concorrentes.");
    }

    private static OnboardingDocumentStatus MapStatus(string asaasStatus) => asaasStatus.ToUpperInvariant() switch
    {
        "AWAITING_APPROVAL" => OnboardingDocumentStatus.AwaitingApproval,
        "APPROVED" => OnboardingDocumentStatus.Approved,
        "REJECTED" => OnboardingDocumentStatus.Rejected,
        _ => OnboardingDocumentStatus.Pending
    };
}
