using Tuilow.Catalog.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;

namespace Tuilow.Catalog.Infrastructure.Services;

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
