using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorSalesHistory;

public sealed class GetCreatorSalesHistoryQueryHandler(ICreatorWalletRepository walletRepository)
    : IRequestHandler<GetCreatorSalesHistoryQuery, IReadOnlyList<WalletTransactionResponse>>
{
    public async Task<IReadOnlyList<WalletTransactionResponse>> Handle(
        GetCreatorSalesHistoryQuery request, CancellationToken ct)
    {
        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(request.CreatorId, ct);
        if (wallet is null) return [];

        var query = wallet.Transactions.AsEnumerable();

        if (request.From is not null)
            query = query.Where(t => t.CreatedAt >= request.From);
        if (request.To is not null)
            query = query.Where(t => t.CreatedAt <= request.To);

        return query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new WalletTransactionResponse(
                t.Id, t.Type.ToString(), t.Status.ToString(),
                t.GrossAmount?.Amount, t.FeeAmount?.Amount, t.NetAmount.Amount,
                t.AppliedFeePercentage, t.ReferenceType, t.ReferenceId,
                t.CycleStart, t.CycleEnd, t.CreatedAt))
            .ToList();
    }
}
