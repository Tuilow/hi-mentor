using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Finance.Domain.Entities;

namespace HiMentor.Finance.Domain.Interfaces;

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

    /// <summary>
    /// Já existe um crédito de venda (WalletTransactionType.SaleCredit) para esta compra? Usado
    /// para tornar CoursePurchaseConfirmedEventHandler (Finance) idempotente a reprocessamento —
    /// achado C2/M1 da auditoria: sem isso, reprocessar manualmente um evento (ver
    /// Sales.Application.Commands.ReprocessCoursePurchase) creditaria a carteira do criador duas
    /// vezes para a mesma compra.
    /// </summary>
    Task<bool> HasSaleTransactionForPurchaseAsync(Guid coursePurchaseId, CancellationToken ct = default);
}
