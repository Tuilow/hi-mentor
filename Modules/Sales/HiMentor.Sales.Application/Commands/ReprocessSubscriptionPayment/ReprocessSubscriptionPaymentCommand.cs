using HiMentor.Sales.Application.Commands.ReprocessCoursePurchase;
using MediatR;

namespace HiMentor.Sales.Application.Commands.ReprocessSubscriptionPayment;

/// <summary>
/// Mesma ideia de ReprocessCoursePurchaseCommand (achado C2 da auditoria), para o fluxo de
/// assinatura: re-publica a confirmação de um pagamento de assinatura JÁ confirmado para
/// destravar matrícula/e-mail quando o processamento original falhou depois do commit.
/// </summary>
public sealed record ReprocessSubscriptionPaymentCommand(Guid SubscriptionId, string AsaasPaymentId) : IRequest<ReprocessResult>;
