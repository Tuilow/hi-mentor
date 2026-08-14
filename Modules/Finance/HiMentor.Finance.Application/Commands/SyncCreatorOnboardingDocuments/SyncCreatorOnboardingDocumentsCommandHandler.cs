using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Application.Interfaces;
using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Commands.SyncCreatorOnboardingDocuments;

/// <summary>
/// Idempotente por natureza (item pedido na correção do bug de persistência abaixo): rodar este
/// comando várias vezes seguidas para o mesmo criador sempre reconcilia pela chave
/// (CreatorAsaasSubaccountId, AsaasDocumentId) — documentos já vistos são atualizados no lugar,
/// nunca duplicados; documentos novos são inseridos uma única vez (garantido em banco pelo índice
/// único da mesma chave, ver CreatorAsaasOnboardingDocumentConfiguration).
///
/// NUNCA chame repository.Update(subaccount) aqui: subaccount.SyncDocuments pode adicionar
/// documentos novos à coleção em memória, e como CreatorAsaasOnboardingDocument.Id é um Guid
/// gerado no cliente (já preenchido no momento da criação), um Update() sobre o agregado inteiro
/// faria o EF Core tratar esses documentos novos como já existentes (Modified) em vez de novos
/// (Added) — gerando um UPDATE para uma linha nunca inserida e DbUpdateConcurrencyException
/// ("esperava afetar 1 linha, afetou 0"). Por isso cada documento novo é registrado
/// explicitamente via repository.AddDocumentAsync — mesmo padrão de
/// ICourseRepository.AddLessonAsync (Catalog, ver AddLessonCommandHandler). O restante do
/// agregado (subaccount em si, e os documentos já existentes atualizados por SyncFrom) já está
/// rastreado pelo EF (carregado sem AsNoTracking em GetByCreatorIdAsync/Include(Documents)) — o
/// próprio SaveChanges detecta as mudanças de propriedade automaticamente, sem precisar de Update().
///
/// Tolerante a chamadas verdadeiramente simultâneas (duas requisições ao endpoint GetDocuments ao
/// mesmo tempo, ex.: duas abas abertas): se ambas descobrirem o mesmo documento novo ao mesmo
/// tempo, a vencedora insere normalmente; a perdedora esbarra no índice único e
/// TrySaveChangesAsync devolve false em vez de propagar a exceção — reconsultamos o registro (que
/// agora já reflete a inserção da vencedora) e tentamos de novo, até um limite pequeno de
/// tentativas. Nenhuma exceção de concorrência é engolida silenciosamente: se as tentativas se
/// esgotarem (cenário extremamente improvável), a falha é propagada normalmente.
/// </summary>
public sealed class SyncCreatorOnboardingDocumentsCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow
) : IRequestHandler<SyncCreatorOnboardingDocumentsCommand, SyncCreatorOnboardingDocumentsResult>
{
    private const int MaxAttempts = 3;

    public async Task<SyncCreatorOnboardingDocumentsResult> Handle(SyncCreatorOnboardingDocumentsCommand request, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
            if (subaccount is null || subaccount.AsaasAccountId is null)
                return new SyncCreatorOnboardingDocumentsResult(false, "NotStarted", "Onboarding financeiro ainda não iniciado.");

            var apiKey = secretProtector.Unprotect(subaccount.ApiKeyEncrypted!);
            var documents = await asaasSubaccountClient.GetPendingDocumentsAsync(apiKey, ct);

            var mapped = documents.Select(d => (
                d.Id, d.Type, d.Title, d.Description, MapStatus(d.Status), d.OnboardingUrl)).ToList();

            var newDocuments = subaccount.SyncDocuments(mapped);
            foreach (var document in newDocuments)
                await repository.AddDocumentAsync(document, ct);

            if (await uow.TrySaveChangesAsync(ct))
                return new SyncCreatorOnboardingDocumentsResult(true, subaccount.Status.ToString(), null);

            // Corrida com outra requisição simultânea (ver documentação da classe) — a próxima
            // iteração reconsulta o registro já com o documento inserido pela vencedora.
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
