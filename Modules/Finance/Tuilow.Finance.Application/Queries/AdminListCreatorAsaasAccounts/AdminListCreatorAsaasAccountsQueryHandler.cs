using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.AdminListCreatorAsaasAccounts;

public sealed class AdminListCreatorAsaasAccountsQueryHandler(
    ICreatorAsaasAccountRepository repository,
    ICreatorDisplayInfoLookup creatorDisplayInfoLookup
) : IRequestHandler<AdminListCreatorAsaasAccountsQuery, IReadOnlyCollection<AdminCreatorAsaasAccountItem>>
{
    public async Task<IReadOnlyCollection<AdminCreatorAsaasAccountItem>> Handle(AdminListCreatorAsaasAccountsQuery request, CancellationToken ct)
    {
        var accounts = (await repository.GetAllAsync(request.Skip, request.Take, ct)).ToList();
        var displayInfo = await creatorDisplayInfoLookup.GetManyAsync(accounts.Select(a => a.CreatorId), ct);

        return accounts.Select(a =>
        {
            displayInfo.TryGetValue(a.CreatorId, out var info);
            return new AdminCreatorAsaasAccountItem(
                a.Id, a.CreatorId, info?.Name ?? "(usuário não encontrado)", info?.Email ?? "—",
                a.Status.ToString(), a.IsEnabledForSelling,
                Mask(a.CpfCnpj), MaskWallet(a.WalletId), a.CommissionOverridePercentage,
                a.LastValidatedAt, a.LastWebhookReceivedAt, a.LastValidationError);
        }).ToList();
    }

    // Mantem so os 3 primeiros e 2 ultimos digitos visiveis -- o suficiente para o admin
    // reconhecer/confirmar sem expor o documento inteiro.
    private static string? Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length <= 5) return new string('*', digits.Length);
        return digits[..3] + new string('*', digits.Length - 5) + digits[^2..];
    }

    private static string? MaskWallet(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        if (value.Length <= 8) return new string('*', value.Length);
        return value[..4] + new string('*', value.Length - 8) + value[^4..];
    }
}
