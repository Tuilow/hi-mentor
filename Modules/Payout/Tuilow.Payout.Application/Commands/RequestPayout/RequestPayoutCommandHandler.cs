using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Finance.Domain.Common;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Payout.Domain.Entities;
using Tuilow.Payout.Domain.Interfaces;
using MediatR;

namespace Tuilow.Payout.Application.Commands.RequestPayout;

public sealed class RequestPayoutCommandHandler(
    IPayoutRequestRepository payoutRequestRepository,
    ICreatorWalletRepository walletRepository,
    IUnitOfWork uow
) : IRequestHandler<RequestPayoutCommand, Guid>
{
    public async Task<Guid> Handle(RequestPayoutCommand request, CancellationToken ct)
    {
        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(request.CreatorId, ct)
            ?? throw new BusinessException("Você ainda não possui vendas registradas nesta carteira.");

        if (await payoutRequestRepository.HasPendingOrApprovedRequestAsync(request.CreatorId, ct))
            throw new BusinessException("Já existe uma solicitação de saque em andamento. Aguarde a conclusão antes de solicitar outra.");

        // Libera saldo de ciclos de 15 dias já fechados antes de avaliar o que pode ser sacado.
        // Os WalletTransaction liberados já vieram rastreados pelo EF via Include — não precisam
        // de Update explícito, só a alteração de Status é suficiente para o change tracker.
        wallet.ReleaseClosedCycles(DateOnly.FromDateTime(DateTime.UtcNow));

        var amount = request.Amount ?? wallet.AvailableBalance.Amount;
        if (amount <= 0 || amount > wallet.AvailableBalance.Amount)
            throw new BusinessException("Saldo disponível insuficiente para este saque.");

        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var payoutRequest = PayoutRequest.Create(request.CreatorId, amount, cycle.Start, cycle.End);
        var walletTransaction = wallet.ReserveForPayout(Money.Of(amount), payoutRequest.Id);

        await payoutRequestRepository.AddAsync(payoutRequest, ct);
        walletRepository.Update(wallet);
        await walletRepository.AddTransactionAsync(walletTransaction, ct);
        await uow.SaveChangesAsync(ct);

        return payoutRequest.Id;
    }
}
