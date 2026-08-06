using MediatR;

namespace Tuilow.Finance.Application.Queries.AdminListCreatorAsaasAccounts;

public sealed record AdminListCreatorAsaasAccountsQuery(int Skip, int Take) : IRequest<IReadOnlyCollection<AdminCreatorAsaasAccountItem>>;

/// <summary>WalletId mascarado (secao 13 da spec: "Wallet ID parcialmente mascarado quando apropriado") -- API Key NUNCA aparece aqui, nem mascarada.</summary>
public sealed record AdminCreatorAsaasAccountItem(
    Guid Id, Guid CreatorId, string Status, bool IsEnabledForSelling,
    string? CpfCnpjMasked, string? WalletIdMasked, decimal? CommissionOverridePercentage,
    DateTime? LastValidatedAt, DateTime? LastWebhookReceivedAt, string? LastValidationError
);
