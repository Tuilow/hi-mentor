using HiMentor.Finance.Application.Interfaces;
using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Queries.AdminListCreatorOnboardings;

/// <summary>
/// Deliberadamente com o próprio helper de mascaramento local (não compartilhado com
/// AdminListCreatorAsaasAccountsQueryHandler, do modelo legado) -- evitou tocar naquele arquivo,
/// que já funciona; a duplicação é pequena (uma função de ~5 linhas) e o custo de mexer no código
/// legado só para reaproveitar isso não valia o risco.
/// </summary>
public sealed class AdminListCreatorOnboardingsQueryHandler(
    ICreatorAsaasSubaccountRepository repository,
    ICreatorDisplayInfoLookup creatorDisplayInfoLookup
) : IRequestHandler<AdminListCreatorOnboardingsQuery, IReadOnlyCollection<AdminCreatorOnboardingItem>>
{
    public async Task<IReadOnlyCollection<AdminCreatorOnboardingItem>> Handle(AdminListCreatorOnboardingsQuery request, CancellationToken ct)
    {
        var subaccounts = (await repository.GetAllAsync(request.Skip, request.Take, ct)).ToList();
        var displayInfo = await creatorDisplayInfoLookup.GetManyAsync(subaccounts.Select(a => a.CreatorId), ct);

        return subaccounts.Select(a =>
        {
            displayInfo.TryGetValue(a.CreatorId, out var info);
            var pendingDocs = a.Documents.Count(d => d.Status == OnboardingDocumentStatus.Pending);
            return new AdminCreatorOnboardingItem(
                a.Id, a.CreatorId, info?.Name ?? "(usuário não encontrado)", info?.Email ?? "—",
                a.Status.ToString(), Mask(a.CpfCnpj), pendingDocs, a.ApprovedAt, a.RejectionReason);
        }).ToList();
    }

    private static string? Mask(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length <= 5) return new string('*', digits.Length);
        return digits[..3] + new string('*', digits.Length - 5) + digits[^2..];
    }
}
