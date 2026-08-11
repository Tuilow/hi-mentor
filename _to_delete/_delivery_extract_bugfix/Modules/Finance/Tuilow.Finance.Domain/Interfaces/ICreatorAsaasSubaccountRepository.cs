using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Finance.Domain.Entities;

namespace Tuilow.Finance.Domain.Interfaces;

public interface ICreatorAsaasSubaccountRepository : IRepository<CreatorAsaasSubaccount>
{
    Task<CreatorAsaasSubaccount?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>Usado pelo webhook de status de conta para descobrir a qual criador um evento pertence, a partir do accountId da Asaas.</summary>
    Task<CreatorAsaasSubaccount?> GetByAsaasAccountIdAsync(string asaasAccountId, CancellationToken ct = default);

    /// <summary>Usado pelo autenticador de webhook de status de conta — mesmo idioma de CreatorAsaasAccount.GetByWebhookTokenHashAsync.</summary>
    Task<CreatorAsaasSubaccount?> GetByWebhookTokenHashAsync(string webhookTokenHash, CancellationToken ct = default);

    Task<IEnumerable<CreatorAsaasSubaccount>> GetAllAsync(int skip, int take, CancellationToken ct = default);

    /// <summary>
    /// Força EntityState.Added para o CreatorAsaasOnboardingDocument — evita
    /// DbUpdateConcurrencyException. Sem isto, o Id (Guid gerado no cliente, já preenchido no
    /// momento da criação — ver CreatorAsaasOnboardingDocument.Create) faria o EF Core tratar um
    /// documento recém-criado como se já existisse no banco, gerando um UPDATE de 0 linhas em vez
    /// de um INSERT. Mesmo padrão de ICourseRepository.AddLessonAsync (Catalog). Chamado pelo
    /// handler para cada documento devolvido por CreatorAsaasSubaccount.SyncDocuments — nunca
    /// chame repository.Update(subaccount) depois de SyncDocuments adicionar documentos novos.
    /// </summary>
    Task AddDocumentAsync(CreatorAsaasOnboardingDocument document, CancellationToken ct = default);
}
