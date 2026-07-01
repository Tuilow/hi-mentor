using DogMaster.Domain.Common.Interfaces;
using SubscriptionEntity = DogMaster.Domain.Contexts.Subscription.Entities.Subscription;
using DogMaster.Domain.Contexts.Subscription.Entities;

namespace DogMaster.Domain.Contexts.Subscription.Interfaces;

public interface ISubscriptionRepository : IRepository<SubscriptionEntity>
{
    Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default);
    Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default);
}
