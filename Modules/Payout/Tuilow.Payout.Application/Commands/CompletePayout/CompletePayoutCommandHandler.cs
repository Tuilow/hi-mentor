using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Payout.Domain.Interfaces;
using MediatR;

namespace Tuilow.Payout.Application.Commands.CompletePayout;

public sealed class CompletePayoutCommandHandler(
    IPayoutRequestRepository payoutRequestRepository,
    ICreatorWalletRepository walletRepository,
    IUnitOfWork uow
) : IRequestHandler<CompletePayoutCommand>
{
    public async Task Handle(CompletePayoutCommand request, CancellationToken ct)
    {
        var payoutRequest = await payoutRequestRepository.GetByIdAsync(request.PayoutRequestId, ct)
            ?? throw new NotFoundException("Solicitação de saque", request.PayoutRequestId);

        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(payoutRequest.CreatorId, ct)
            ?? throw new NotFoundException("Carteira do criador", payoutRequest.CreatorId);

        var payoutTransaction = payoutRequest.MarkPaid(request.ExternalReference);
        var walletTransaction = wallet.ConfirmPayoutCompleted(payoutRequest.RequestedAmount, payoutRequest.Id);

        payoutRequestRepository.Update(payoutRequest);
        await payoutRequestRepository.AddTransactionAsync(payoutTransaction, ct);

        walletRepository.Update(wallet);
        await walletRepository.AddTransactionAsync(walletTransaction, ct);

        await uow.SaveChangesAsync(ct);
    }
}
