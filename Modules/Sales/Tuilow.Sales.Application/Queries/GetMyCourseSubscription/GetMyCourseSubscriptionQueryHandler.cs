using Tuilow.Sales.Application.Queries.GetUserSubscription;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Sales.Application.Queries.GetMyCourseSubscription;

public sealed class GetMyCourseSubscriptionQueryHandler(ISubscriptionRepository repo)
    : IRequestHandler<GetMyCourseSubscriptionQuery, UserSubscriptionResponse?>
{
    public async Task<UserSubscriptionResponse?> Handle(GetMyCourseSubscriptionQuery request, CancellationToken ct)
    {
        var sub = await repo.GetActiveByUserForCourseAsync(request.UserId, request.CourseId, ct);
        if (sub is null) return null;

        var plan = await repo.GetPlanByIdAsync(sub.PlanId, ct);
        if (plan is null) return null;

        return new UserSubscriptionResponse(
            sub.Id, plan.Name, sub.Status.ToString(),
            plan.Price.Amount, sub.BillingCycle.ToString(),
            sub.CurrentPeriodEnd, sub.IsActive);
    }
}
