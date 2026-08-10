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
}
