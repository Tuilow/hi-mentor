using Tuilow.Domain.Common.Interfaces;
using SubscriptionEntity = Tuilow.Domain.Contexts.Subscription.Entities.Subscription;
using Tuilow.Domain.Contexts.Subscription.Entities;

namespace Tuilow.Domain.Contexts.Subscription.Interfaces;

public interface ISubscriptionRepository : IRepository<SubscriptionEntity>
{
    Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default);
    Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default);
}
