using MediatR;

namespace HiMentor.Finance.Application.Commands.AdminSetCreatorAsaasAccountEnabled;

/// <summary>Liga/desliga manualmente a capacidade de um creator vender via marketplace (ex.: suspeita de fraude, pedido do proprio creator) -- nao mexe na validacao da Asaas em si.</summary>
public sealed record AdminSetCreatorAsaasAccountEnabledCommand(Guid CreatorAsaasAccountId, bool Enabled) : IRequest;
