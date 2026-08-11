using Tuilow.Sales.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>Implementação real de <see cref="ICreatorPaymentAccountLookup"/> — consulta o módulo Finance.</summary>
public sealed class FinanceCreatorPaymentAccountLookup(
    ICreatorAsaasAccountRepository creatorAsaasAccountRepository,
    IPlatformFeeConfigurationRepository platformFeeConfigurationRepository,
    // Novo modelo de onboarding (subconta Asaas/BaaS) -- ver HasApprovedFinancialOnboardingAsync.
    ICreatorAsaasSubaccountRepository creatorAsaasSubaccountRepository
) : ICreatorPaymentAccountLookup
{
    // Mesmo valor de Tuilow.Finance.Application.EventHandlers.CoursePurchaseConfirmedEventHandler.DefaultFeePercentage
    // (fallback do modelo Legacy) -- duplicado aqui de propósito para não criar uma referência de
    // Sales.Infrastructure para a camada Application de Finance (só Domain é acoplamento legítimo
    // nesta base, ver demais comentários de ProjectReference). Se o padrão da plataforma mudar,
    // atualizar os dois lugares.
    private const decimal DefaultFeePercentage = 10m;

    // Corrigido em ago/2026 (bug de produção: webhook de pagamento rejeitado por
    // divergência de conta) -- este método decide qual conta é usada para CRIAR a cobrança/cliente
    // no marketplace (ver PurchaseCourseCommandHandler -> AsaasMarketplacePaymentService), então
    // precisa apontar para o MESMO modelo que HasApprovedFinancialOnboardingAsync já usa para
    // aprovar o creator (CreatorAsaasSubaccount, o novo modelo de subconta BaaS) -- nunca mais para
    // o modelo legado CreatorAsaasAccount ("cole sua API Key"), que fica só para creators antigos
    // ainda não migrados.
    public async Task<CreatorMarketplaceAccountInfo?> GetMarketplaceAccountAsync(Guid creatorId, CancellationToken ct = default)
    {
        var subaccount = await creatorAsaasSubaccountRepository.GetByCreatorIdAsync(creatorId, ct);
        return subaccount is null ? null : new CreatorMarketplaceAccountInfo(subaccount.Id, subaccount.CanSell);
    }

    public async Task<decimal> GetEffectiveCommissionPercentageAsync(Guid creatorId, CancellationToken ct = default)
    {
        // Precedência: override específico do creator -> percentual padrão da plataforma vigente
        // -> fallback hardcoded (mesmo valor usado pelo modelo Legacy, CoursePurchaseConfirmedEventHandler.DefaultFeePercentage).
        var account = await creatorAsaasAccountRepository.GetByCreatorIdAsync(creatorId, ct);
        if (account?.CommissionOverridePercentage is decimal overridePercentage)
            return overridePercentage;

        var feeConfig = await platformFeeConfigurationRepository.GetActiveAsync(ct);
        return feeConfig?.Percentage ?? DefaultFeePercentage;
    }

    public async Task<bool> HasApprovedFinancialOnboardingAsync(Guid creatorId, CancellationToken ct = default)
    {
        var subaccount = await creatorAsaasSubaccountRepository.GetByCreatorIdAsync(creatorId, ct);
        return subaccount?.CanSell ?? false;
    }
}
