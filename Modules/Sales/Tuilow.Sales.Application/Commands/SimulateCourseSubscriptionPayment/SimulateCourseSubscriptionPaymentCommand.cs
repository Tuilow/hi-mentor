using MediatR;

namespace Tuilow.Sales.Application.Commands.SimulateCourseSubscriptionPayment;

/// <summary>
/// Uso exclusivo de sandbox/desenvolvimento — espelha
/// <see cref="Tuilow.Sales.Application.Commands.SimulateCoursePurchasePayment.SimulateCoursePurchasePaymentCommand"/>,
/// só que para uma assinatura de produto (ver SubscribeToCourseCommandHandler) em vez de uma
/// compra avulsa. UserId é opcional pelo mesmo motivo: no checkout anônimo a conta pode ter sido
/// criada automaticamente, sem senha, então quem simula o pagamento em dev pode não estar logado
/// como o assinante.
/// </summary>
public sealed record SimulateCourseSubscriptionPaymentCommand(Guid? UserId, Guid SubscriptionId) : IRequest;
