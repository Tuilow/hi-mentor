using MediatR;

namespace Tuilow.Finance.Application.Queries.GetMyAsaasAccountStatus;

public sealed record GetMyAsaasAccountStatusQuery(Guid CreatorId) : IRequest<CreatorAsaasAccountStatusResponse>;

public sealed record CreatorAsaasAccountStatusResponse(
    bool IsConnected,
    string Status,
    bool CanSell,
    string? WalletId,
    decimal? CommissionOverridePercentage,
    DateTime? LastValidatedAt,
    string? LastValidationError
);
