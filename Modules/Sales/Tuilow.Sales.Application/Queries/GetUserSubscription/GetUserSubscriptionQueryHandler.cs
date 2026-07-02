using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Sales.Application.Queries.GetUserSubscription;

public sealed class GetUserSubscriptionQueryHandler(ISubscriptionRepository repo)
    : IRequestHandler<GetUserSubscriptionQuery, UserSubscriptionResponse?>
{
    public async Task<UserSubscriptionResponse?> Handle(GetUserSubscriptionQuery request, CancellationToken ct)
    {
        var sub = await repo.GetActiveByUserAsync(request.UserId, ct);
        if (sub is null) return null;

        var plan = await repo.GetPlanByIdAsync(sub.PlanId, ct);
        if (plan is null) return null;

        return new UserSubscriptionResponse(
            sub.Id, plan.Name, sub.Status.ToString(),
            plan.Price.Amount, sub.BillingCycle.ToString(),
            sub.CurrentPeriodEnd, sub.IsActive);
    }
}
