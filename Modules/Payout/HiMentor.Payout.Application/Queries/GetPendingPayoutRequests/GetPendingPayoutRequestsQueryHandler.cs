using HiMentor.Payout.Domain.Interfaces;
using MediatR;

namespace HiMentor.Payout.Application.Queries.GetPendingPayoutRequests;

public sealed class GetPendingPayoutRequestsQueryHandler(IPayoutRequestRepository repository)
    : IRequestHandler<GetPendingPayoutRequestsQuery, IReadOnlyList<AdminPayoutRequestResponse>>
{
    public async Task<IReadOnlyList<AdminPayoutRequestResponse>> Handle(GetPendingPayoutRequestsQuery request, CancellationToken ct)
    {
        var requests = await repository.GetPendingAsync(ct);

        return requests
            .OrderBy(r => r.RequestedAt)
            .Select(r => new AdminPayoutRequestResponse(
                r.Id, r.CreatorId, r.RequestedAmount.Amount, r.Status.ToString(),
                r.CycleStart, r.CycleEnd, r.RequestedAt))
            .ToList();
    }
}
