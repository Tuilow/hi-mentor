using Tuilow.Sales.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>Lê o kill-switch global do marketplace de split de "Asaas:MarketplaceSplitEnabled" (appsettings/env). Padrão: desligado (opt-in explícito antes de qualquer venda real passar a usar o modelo novo).</summary>
public sealed class ConfigMarketplaceFeatureFlag(IConfiguration configuration) : IMarketplaceFeatureFlag
{
    public bool IsEnabled => configuration.GetValue("Asaas:MarketplaceSplitEnabled", false);
}
