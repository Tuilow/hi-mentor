using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Finance.Domain.Entities;

namespace HiMentor.Finance.Domain.Interfaces;

public interface ICreatorAsaasAccountRepository : IRepository<CreatorAsaasAccount>
{
    Task<CreatorAsaasAccount?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>
    /// Busca pelo hash do token de webhook -- usado pelo AsaasWebhookController para descobrir a
    /// qual creator um webhook recebido pertence, sem precisar decriptar nenhuma API Key
    /// (comparacao e sempre hash-a-hash). Ver CreatorAsaasAccount.WebhookTokenHash.
    /// </summary>
    Task<CreatorAsaasAccount?> GetByWebhookTokenHashAsync(string webhookTokenHash, CancellationToken ct = default);

    Task<IEnumerable<CreatorAsaasAccount>> GetAllAsync(int skip, int take, CancellationToken ct = default);
}
