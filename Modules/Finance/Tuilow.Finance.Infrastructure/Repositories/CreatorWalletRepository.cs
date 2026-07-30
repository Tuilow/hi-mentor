using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Finance.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.</summary>
public sealed class CreatorWalletRepository(DbContext context) : ICreatorWalletRepository
{
    public async Task<CreatorWallet?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<CreatorWallet>().Include(w => w.Transactions).FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<CreatorWallet>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<CreatorWallet>().ToListAsync(ct);

    public async Task AddAsync(CreatorWallet entity, CancellationToken ct = default) =>
        await context.Set<CreatorWallet>().AddAsync(entity, ct);

    public void Update(CreatorWallet entity) => context.Set<CreatorWallet>().Update(entity);
    public void Delete(CreatorWallet entity) => context.Set<CreatorWallet>().Remove(entity);

    public async Task<CreatorWallet?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorWallet>().FirstOrDefaultAsync(w => w.CreatorId == creatorId, ct);

    public async Task<CreatorWallet?> GetByCreatorIdWithTransactionsAsync(Guid creatorId, CancellationToken ct = default) =>
        await context.Set<CreatorWallet>()
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.CreatorId == creatorId, ct);

    /// <summary>
    /// Registra o WalletTransaction explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified
    /// (mesmo padrão de Sales.AddPaymentAsync / Learning.AddLessonProgressAsync).
    /// </summary>
    public async Task AddTransactionAsync(WalletTransaction transaction, CancellationToken ct = default) =>
        await context.Set<WalletTransaction>().AddAsync(transaction, ct);

    public async Task<(decimal GrossTotal, decimal FeeTotal, decimal NetTotal, int SalesCount)> GetPlatformTotalsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = context.Set<WalletTransaction>()
            .Where(t => t.Type == WalletTransactionType.SaleCredit);

        if (from is not null) query = query.Where(t => t.CreatedAt >= from);
        if (to is not null) query = query.Where(t => t.CreatedAt <= to);

        var rows = await query
            .Select(t => new { Gross = t.GrossAmount!.Amount, Fee = t.FeeAmount!.Amount, Net = t.NetAmount.Amount })
            .ToListAsync(ct);

        return (rows.Sum(r => r.Gross), rows.Sum(r => r.Fee), rows.Sum(r => r.Net), rows.Count);
    }

    public async Task<bool> HasSaleTransactionForPurchaseAsync(Guid coursePurchaseId, CancellationToken ct = default) =>
        await context.Set<WalletTransaction>().AnyAsync(t =>
            t.Type == WalletTransactionType.SaleCredit
            && t.ReferenceType == "CoursePurchase"
            && t.ReferenceId == coursePurchaseId, ct);
}
