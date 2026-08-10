namespace Tuilow.Sales.Application.Interfaces;

public sealed record CreatorMarketplaceAccountInfo(Guid CreatorAsaasAccountId, bool CanSell);

/// <summary>
/// Porta (anti-corruption layer) que abstrai "este creator tem uma conta Asaas propria
/// conectada e apta a vender?" sem o modulo Sales depender diretamente do dominio de Finance --
/// mesmo padrao de <see cref="IWalletCreditChecker"/>. Implementada em Sales.Infrastructure
/// consultando Finance.Domain (mesmo acoplamento ja existente ali).
/// </summary>
public interface ICreatorPaymentAccountLookup
{
    Task<CreatorMarketplaceAccountInfo?> GetMarketplaceAccountAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>Precedencia: override de comissao do proprio creator -&gt; percentual padrao da plataforma vigente.</summary>
    Task<decimal> GetEffectiveCommissionPercentageAsync(Guid creatorId, CancellationToken ct = default);

    /// <summary>
    /// Onboarding financeiro do novo modelo (subconta Asaas/BaaS criada pela Tuilow, ver
    /// CreatorAsaasSubaccount em Finance.Domain) aprovado para este creator? Usado como defesa em
    /// profundidade em PurchaseCourseCommandHandler -- o bloqueio "de verdade" já acontece na
    /// publicação (PublishCourseCommandHandler/PublishProductCommandHandler), então em condições
    /// normais nenhuma compra deveria chegar aqui sem isso ser true; este check existe só para
    /// nunca deixar a venda cair silenciosamente no fallback Legacy (conta da própria Tuilow) para
    /// um creator que não passou pelo novo onboarding -- decisão explícita: bloqueio vale para
    /// todos os creators, imediatamente, sem exceção para quem já vendia pelo modelo antigo.
    /// </summary>
    Task<bool> HasApprovedFinancialOnboardingAsync(Guid creatorId, CancellationToken ct = default);
}
