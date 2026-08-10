using MediatR;

namespace Tuilow.Finance.Application.Commands.AdminSetCreatorOnboardingBlocked;

/// <summary>Bloqueia/desbloqueia manualmente a venda de um criador (suspeita de fraude, pedido do próprio criador) — mesmo padrão de AdminSetCreatorAsaasAccountEnabledCommand no modelo legado.</summary>
public sealed record AdminSetCreatorOnboardingBlockedCommand(Guid CreatorAsaasSubaccountId, bool Blocked, string? Reason) : IRequest;
