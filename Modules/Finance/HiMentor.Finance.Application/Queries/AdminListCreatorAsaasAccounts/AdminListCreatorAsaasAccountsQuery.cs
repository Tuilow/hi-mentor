using MediatR;

namespace HiMentor.Finance.Application.Queries.AdminListCreatorAsaasAccounts;

public sealed record AdminListCreatorAsaasAccountsQuery(int Skip, int Take) : IRequest<IReadOnlyCollection<AdminCreatorAsaasAccountItem>>;

/// <summary>WalletId/CPF mascarados (secao 13 da spec) -- API Key NUNCA aparece aqui, nem mascarada.</summary>
public sealed record AdminCreatorAsaasAccountItem(
    Guid Id, Guid CreatorId, string CreatorName, string CreatorEmail,
    string Status, bool IsEnabledForSelling,
    string? CpfCnpjMasked, string? WalletIdMasked, decimal? CommissionOverridePercentage,
    DateTime? LastValidatedAt, DateTime? LastWebhookReceivedAt, string? LastValidationError
);
