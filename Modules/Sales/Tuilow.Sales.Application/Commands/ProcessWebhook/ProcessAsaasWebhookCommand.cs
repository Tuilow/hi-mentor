using MediatR;

namespace Tuilow.Sales.Application.Commands.ProcessWebhook;

public sealed record AsaasWebhookPayload(
    string Event,
    AsaasPaymentData Payment
);

public sealed record AsaasPaymentData(
    string Id,
    string? Subscription,
    decimal Value,
    string Status
);

public sealed record ProcessAsaasWebhookCommand(AsaasWebhookPayload Payload) : IRequest;
