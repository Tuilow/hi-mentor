using Tuilow.SharedKernel.Domain.Interfaces;
using SubscriptionEntity = Tuilow.Sales.Domain.Entities.Subscription;
using Tuilow.Sales.Domain.Entities;

namespace Tuilow.Sales.Domain.Interfaces;

public interface ISubscriptionRepository : IRepository<SubscriptionEntity>
{
    Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default);
    Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o SubscriptionPayment — evita DbUpdateConcurrencyException.</summary>
    Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default);
}
