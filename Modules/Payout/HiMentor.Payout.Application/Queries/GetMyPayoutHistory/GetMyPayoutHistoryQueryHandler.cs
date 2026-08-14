using HiMentor.Payout.Domain.Interfaces;
using MediatR;

namespace HiMentor.Payout.Application.Queries.GetMyPayoutHistory;

public sealed class GetMyPayoutHistoryQueryHandler(IPayoutRequestRepository repository)
    : IRequestHandler<GetMyPayoutHistoryQuery, IReadOnlyList<PayoutRequestResponse>>
{
    public async Task<IReadOnlyList<PayoutRequestResponse>> Handle(GetMyPayoutHistoryQuery request, CancellationToken ct)
    {
        var requests = await repository.GetByCreatorAsync(request.CreatorId, ct);

        return requests
            .OrderByDescending(r => r.RequestedAt)
            .Select(r => new PayoutRequestResponse(
                r.Id, r.RequestedAmount.Amount, r.Status.ToString(),
                r.CycleStart, r.CycleEnd, r.RequestedAt, r.ReviewedAt, r.RejectionReason, r.PaidAt))
            .ToList();
    }
}
