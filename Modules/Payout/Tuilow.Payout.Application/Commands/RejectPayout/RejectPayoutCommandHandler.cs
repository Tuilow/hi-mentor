using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Payout.Domain.Interfaces;
using MediatR;

namespace Tuilow.Payout.Application.Commands.RejectPayout;

public sealed class RejectPayoutCommandHandler(
    IPayoutRequestRepository payoutRequestRepository,
    ICreatorWalletRepository walletRepository,
    IUnitOfWork uow
) : IRequestHandler<RejectPayoutCommand>
{
    public async Task Handle(RejectPayoutCommand request, CancellationToken ct)
    {
        var payoutRequest = await payoutRequestRepository.GetByIdAsync(request.PayoutRequestId, ct)
            ?? throw new NotFoundException("Solicitação de saque", request.PayoutRequestId);

        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(payoutRequest.CreatorId, ct)
            ?? throw new NotFoundException("Carteira do criador", payoutRequest.CreatorId);

        payoutRequest.Reject(request.AdminUserId, request.Reason);
        var walletTransaction = wallet.ReleaseReservedFunds(payoutRequest.RequestedAmount, payoutRequest.Id);

        payoutRequestRepository.Update(payoutRequest);
        walletRepository.Update(wallet);
        await walletRepository.AddTransactionAsync(walletTransaction, ct);
        await uow.SaveChangesAsync(ct);
    }
}
