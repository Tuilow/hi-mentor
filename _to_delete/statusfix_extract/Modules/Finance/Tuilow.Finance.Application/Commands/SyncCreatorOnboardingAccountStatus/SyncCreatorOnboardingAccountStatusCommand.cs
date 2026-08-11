using MediatR;

namespace Tuilow.Finance.Application.Commands.SyncCreatorOnboardingAccountStatus;

/// <summary>
/// Rede de segurança para o veredito GERAL de aprovação do onboarding financeiro (ver
/// CreatorAsaasSubaccount.ApplyAccountStatusSync). Hoje esse veredito só chega via webhook
/// (ACCOUNT_STATUS_GENERAL_APPROVAL_* — ver ProcessAsaasAccountStatusWebhookCommandHandler); se o
/// registro do webhook falhou silenciosamente na criação da subconta (ver
/// StartCreatorFinancialOnboardingCommandHandler, log crítico) ou a entrega falhar por qualquer
/// motivo de rede, o criador fica preso no passo "Análise" para sempre mesmo já aprovado de
/// verdade na Asaas — foi exatamente o que aconteceu em produção (creator aprovado na Asaas,
/// GetMyFinancialOnboardingStatusQuery continuava devolvendo UnderReview indefinidamente).
///
/// Este comando é best-effort por design: nunca lança para o chamador por falha de rede/Asaas
/// (só por bug genuíno) -- é enfileirado a cada carregamento da tela de status
/// (CreatorFinancialOnboardingController.GetStatus), então uma falha aqui não pode derrubar a
/// tela inteira. Throttlado no handler para não bater na Asaas a cada poll.
/// </summary>
public sealed record SyncCreatorOnboardingAccountStatusCommand(Guid CreatorId) : IRequest;
