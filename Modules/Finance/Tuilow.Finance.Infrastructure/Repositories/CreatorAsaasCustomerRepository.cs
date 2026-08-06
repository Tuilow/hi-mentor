using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Finance.Infrastructure.Repositories;

public sealed class CreatorAsaasCustomerRepository(DbContext context) : ICreatorAsaasCustomerRepository
{
    public async Task<CreatorAsaasCustomer?> GetAsync(Guid creatorAsaasAccountId, Guid studentId, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasCustomer>()
            .FirstOrDefaultAsync(c => c.CreatorAsaasAccountId == creatorAsaasAccountId && c.StudentId == studentId, ct);

    public async Task AddAsync(CreatorAsaasCustomer customer, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasCustomer>().AddAsync(customer, ct);
}
