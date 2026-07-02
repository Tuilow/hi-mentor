using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Payout.Domain.Entities;

namespace Tuilow.Payout.Domain.Interfaces;

public interface IPayoutRequestRepository : IRepository<PayoutRequest>
{
    Task<bool> HasPendingOrApprovedRequestAsync(Guid creatorId, CancellationToken ct = default);
    Task<IEnumerable<PayoutRequest>> GetByCreatorAsync(Guid creatorId, CancellationToken ct = default);
    Task<IEnumerable<PayoutRequest>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o PayoutTransaction — evita DbUpdateConcurrencyException.</summary>
    Task AddTransactionAsync(PayoutTransaction transaction, CancellationToken ct = default);
}
