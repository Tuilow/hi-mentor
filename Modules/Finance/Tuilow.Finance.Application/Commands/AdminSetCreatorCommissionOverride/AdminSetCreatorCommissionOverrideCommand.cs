using MediatR;

namespace Tuilow.Finance.Application.Commands.AdminSetCreatorCommissionOverride;

/// <summary>Define (ou remove, se Percentage for nulo) um percentual de comissao especifico para este creator -- precedencia sobre o padrao da plataforma (PlatformFeeConfiguration) nas proximas vendas MarketplaceSplit dele. Nao afeta vendas ja realizadas (snapshot).</summary>
public sealed record AdminSetCreatorCommissionOverrideCommand(Guid CreatorAsaasAccountId, decimal? Percentage) : IRequest;
