using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Subscription.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DogMaster.Application.Contexts.Subscription.Commands.ProcessWebhook;

public sealed class ProcessAsaasWebhookCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork uow,
    ILogger<ProcessAsaasWebhookCommandHandler> logger
) : IRequestHandler<ProcessAsaasWebhookCommand>
{
    public async Task Handle(ProcessAsaasWebhookCommand request, CancellationToken ct)
    {
        var payload = request.Payload;

        if (string.IsNullOrEmpty(payload.Payment.Subscription))
        {
            logger.LogInformation("Webhook Asaas sem subscription ID: {PaymentId}", payload.Payment.Id);
            return;
        }

        var subscription = await subscriptionRepository
            .GetByAsaasSubscriptionIdAsync(payload.Payment.Subscription, ct);

        if (subscription is null)
        {
            logger.LogWarning("Assinatura não encontrada para ID Asaas: {Id}", payload.Payment.Subscription);
            return;
        }

        switch (payload.Event)
        {
            case "PAYMENT_RECEIVED":
            case "PAYMENT_CONFIRMED":
                subscription.ConfirmPayment(payload.Payment.Id, payload.Payment.Value);
                logger.LogInformation("Pagamento confirmado: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                subscription.MarkPaymentFailed(payload.Payment.Id, payload.Payment.Value);
                logger.LogWarning("Pagamento falhou: {PaymentId}", payload.Payment.Id);
                break;

            default:
                logger.LogDebug("Evento Asaas ignorado: {Event}", payload.Event);
                return;
        }

        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }
}
