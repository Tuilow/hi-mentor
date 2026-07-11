using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Payout.Domain.Interfaces;
using MediatR;

namespace Tuilow.Payout.Application.Commands.ApprovePayout;

public sealed class ApprovePayoutCommandHandler(
    IPayoutRequestRepository repository,
    IUnitOfWork uow
) : IRequestHandler<ApprovePayoutCommand>
{
    public async Task Handle(ApprovePayoutCommand request, CancellationToken ct)
    {
        var payoutRequest = await repository.GetByIdAsync(request.PayoutRequestId, ct)
            ?? throw new NotFoundException("Solicitação de saque", request.PayoutRequestId);

        payoutRequest.Approve(request.AdminUserId);
        repository.Update(payoutRequest);
        await uow.SaveChangesAsync(ct);
    }
}
