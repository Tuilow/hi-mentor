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
}
