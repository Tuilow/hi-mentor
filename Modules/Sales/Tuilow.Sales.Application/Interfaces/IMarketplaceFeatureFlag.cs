namespace Tuilow.Sales.Application.Interfaces;

/// <summary>
/// Kill-switch global do marketplace de split de pagamentos (rollout controlado). Mesmo com uma
/// CreatorAsaasAccount ativa, nenhuma venda nova usa o modelo MarketplaceSplit se este flag
/// estiver desligado -- todas caem no modelo Legacy (comportamento anterior, inalterado).
/// Configuravel via "Asaas:MarketplaceSplitEnabled" (appsettings/env — ver Program.cs).
/// </summary>
public interface IMarketplaceFeatureFlag
{
    bool IsEnabled { get; }
}
