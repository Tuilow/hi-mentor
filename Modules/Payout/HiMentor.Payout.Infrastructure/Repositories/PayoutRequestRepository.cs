using HiMentor.Payout.Domain.Entities;
using HiMentor.Payout.Domain.Enums;
using HiMentor.Payout.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Payout.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.</summary>
public sealed class PayoutRequestRepository(DbContext context) : IPayoutRequestRepository
{
    public async Task<PayoutRequest?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<PayoutRequest>().Include(p => p.Transactions).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IEnumerable<PayoutRequest>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<PayoutRequest>().ToListAsync(ct);

    public async Task AddAsync(PayoutRequest entity, CancellationToken ct = default) =>
        await context.Set<PayoutRequest>().AddAsync(entity, ct);

    public void Update(PayoutRequest entity) => context.Set<PayoutRequest>().Update(entity);
    public void Delete(PayoutRequest entity) => context.Set<PayoutRequest>().Remove(entity);

    public async Task<bool> HasPendingOrApprovedRequestAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<PayoutRequest>().AnyAsync(p =>
            p.CreatorId == creatorId &&
            (p.Status == PayoutRequestStatus.Pending || p.Status == PayoutRequestStatus.Approved), ct);

    public async Task<IEnumerable<PayoutRequest>> GetByCreatorAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<PayoutRequest>()
            .Where(p => p.CreatorId == creatorId)
            .OrderByDescending(p => p.RequestedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<PayoutRequest>> GetPendingAsync(CancellationToken ct = default) =>
        await context.Set<PayoutRequest>()
            .Where(p => p.Status == PayoutRequestStatus.Pending)
            .OrderBy(p => p.RequestedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Registra o PayoutTransaction explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified.
    /// </summary>
    public async Task AddTransactionAsync(PayoutTransaction transaction, CancellationToken ct = default) =>
        await context.Set<PayoutTransaction>().AddAsync(transaction, ct);
}
