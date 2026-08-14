using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Payout.Domain.Interfaces;
using MediatR;

namespace HiMentor.Payout.Application.Commands.ApprovePayout;

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
