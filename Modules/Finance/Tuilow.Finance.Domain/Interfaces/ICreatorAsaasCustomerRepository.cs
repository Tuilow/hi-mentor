using Tuilow.Finance.Domain.Entities;

namespace Tuilow.Finance.Domain.Interfaces;

public interface ICreatorAsaasCustomerRepository
{
    Task<CreatorAsaasCustomer?> GetAsync(Guid creatorAsaasAccountId, Guid studentId, CancellationToken ct = default);
    Task AddAsync(CreatorAsaasCustomer customer, CancellationToken ct = default);
}
