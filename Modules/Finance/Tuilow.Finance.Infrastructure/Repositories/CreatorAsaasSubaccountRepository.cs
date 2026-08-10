using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Finance.Infrastructure.Repositories;

public sealed class CreatorAsaasSubaccountRepository(DbContext context) : ICreatorAsaasSubaccountRepository
{
    public async Task<CreatorAsaasSubaccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<CreatorAsaasSubaccount>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().ToListAsync(ct);

    public async Task<IEnumerable<CreatorAsaasSubaccount>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>()
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(CreatorAsaasSubaccount entity, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().AddAsync(entity, ct);

    public void Update(CreatorAsaasSubaccount entity) => context.Set<CreatorAsaasSubaccount>().Update(entity);
    public void Delete(CreatorAsaasSubaccount entity) => context.Set<CreatorAsaasSubaccount>().Remove(entity);

    public async Task<CreatorAsaasSubaccount?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().Include(a => a.Documents).FirstOrDefaultAsync(a => a.CreatorId == creatorId, ct);

    public async Task<CreatorAsaasSubaccount?> GetByAsaasAccountIdAsync(string asaasAccountId, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().FirstOrDefaultAsync(a => a.AsaasAccountId == asaasAccountId, ct);

    public async Task<CreatorAsaasSubaccount?> GetByWebhookTokenHashAsync(string webhookTokenHash, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasSubaccount>().FirstOrDefaultAsync(a => a.WebhookTokenHash == webhookTokenHash, ct);
}
