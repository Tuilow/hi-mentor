using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Finance.Domain.Entities;

namespace Tuilow.Finance.Domain.Interfaces;

public interface ICreatorWalletRepository : IRepository<CreatorWallet>
{
    Task<CreatorWallet?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>Carrega a carteira com o extrato completo de lançamentos (para dashboard/histórico).</summary>
    Task<CreatorWallet?> GetByCreatorIdWithTransactionsAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o WalletTransaction — evita DbUpdateConcurrencyException.</summary>
    Task AddTransactionAsync(WalletTransaction transaction, CancellationToken ct = default);

    /// <summary>Soma o valor bruto de vendas de todos os criadores em um intervalo — receita da plataforma (admin).</summary>
    Task<(decimal GrossTotal, decimal FeeTotal, decimal NetTotal, int SalesCount)> GetPlatformTotalsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default);
}
