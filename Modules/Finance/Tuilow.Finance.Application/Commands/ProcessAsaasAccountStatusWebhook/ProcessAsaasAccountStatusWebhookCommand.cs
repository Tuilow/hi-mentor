using MediatR;

namespace Tuilow.Finance.Application.Commands.ProcessAsaasAccountStatusWebhook;

public sealed record AsaasAccountStatusPayload(
    string Id, // id do evento -- usado para dedupe (ver ProcessedAsaasAccountEvent)
    string Event,
    AsaasAccountRef Account
);

public sealed record AsaasAccountRef(string Id);

public sealed record ProcessAsaasAccountStatusWebhookCommand(AsaasAccountStatusPayload Payload) : IRequest;
