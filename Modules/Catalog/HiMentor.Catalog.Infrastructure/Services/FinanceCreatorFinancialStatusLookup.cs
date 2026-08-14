using HiMentor.Catalog.Application.Interfaces;
using HiMentor.Finance.Domain.Interfaces;

namespace HiMentor.Catalog.Infrastructure.Services;

/// <summary>Implementação real de <see cref="ICreatorFinancialStatusLookup"/> — consulta o módulo Finance (CreatorAsaasSubaccount, novo modelo de onboarding via subconta).</summary>
public sealed class FinanceCreatorFinancialStatusLookup(
    ICreatorAsaasSubaccountRepository repository
) : ICreatorFinancialStatusLookup
{
    public async Task<bool> CanSellAsync(Guid creatorId, CancellationToken ct = default)
    {
        var subaccount = await repository.GetByCreatorIdAsync(creatorId, ct);
        return subaccount?.CanSell ?? false;
    }
}
