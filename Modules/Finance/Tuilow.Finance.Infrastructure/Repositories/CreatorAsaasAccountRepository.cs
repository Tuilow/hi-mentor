using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Finance.Infrastructure.Repositories;

public sealed class CreatorAsaasAccountRepository(DbContext context) : ICreatorAsaasAccountRepository
{
    public async Task<CreatorAsaasAccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>().FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IEnumerable<CreatorAsaasAccount>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>().ToListAsync(ct);

    public async Task<IEnumerable<CreatorAsaasAccount>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>()
            .OrderByDescending(a => a.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(CreatorAsaasAccount entity, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>().AddAsync(entity, ct);

    public void Update(CreatorAsaasAccount entity) => context.Set<CreatorAsaasAccount>().Update(entity);
    public void Delete(CreatorAsaasAccount entity) => context.Set<CreatorAsaasAccount>().Remove(entity);

    public async Task<CreatorAsaasAccount?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>().FirstOrDefaultAsync(a => a.CreatorId == creatorId, ct);

    public async Task<CreatorAsaasAccount?> GetByWebhookTokenHashAsync(string webhookTokenHash, CancellationToken ct = default) =>
        await context.Set<CreatorAsaasAccount>().FirstOrDefaultAsync(a => a.WebhookTokenHash == webhookTokenHash, ct);
}
