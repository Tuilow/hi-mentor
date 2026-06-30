using DogMaster.Domain.Contexts.Subscription.Enums;
using MediatR;

namespace DogMaster.Application.Contexts.Subscription.Commands.CreateSubscription;

public sealed record CreateSubscriptionCommand(
    Guid UserId,
    Guid PlanId,
    string CustomerName,
    string CustomerEmail,
    string? CpfCnpj = null,
    string? Phone = null
) : IRequest<CreateSubscriptionResponse>;

public sealed record CreateSubscriptionResponse(Guid SubscriptionId, string AsaasSubscriptionId);
