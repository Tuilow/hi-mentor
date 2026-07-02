using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorSalesHistory;

public sealed record GetCreatorSalesHistoryQuery(Guid CreatorId, DateTime? From, DateTime? To) : IRequest<IReadOnlyList<WalletTransactionResponse>>;

public sealed record WalletTransactionResponse(
    Guid Id,
    string Type,
    string Status,
    decimal? GrossAmount,
    decimal? FeeAmount,
    decimal NetAmount,
    decimal? AppliedFeePercentage,
    string? ReferenceType,
    Guid? ReferenceId,
    DateOnly CycleStart,
    DateOnly CycleEnd,
    DateTime CreatedAt
);
