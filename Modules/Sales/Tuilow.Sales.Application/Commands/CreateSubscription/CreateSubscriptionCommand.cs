using MediatR;

namespace Tuilow.Sales.Application.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    Guid? UserId,
    Guid PlanId,
    string CustomerName,
    string CustomerEmail,
    string? CpfCnpj = null,
    string? Phone = null
) : IRequest<CreateSubscriptionResponse>;

public sealed record CreateSubscriptionResponse(
    Guid SubscriptionId,
    string AsaasSubscriptionId,
    string? PaymentUrl   // invoiceUrl do Asaas — onde o cliente paga via PIX/cartão/boleto
);
