using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Application.Commands.ProcessWebhook;

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

        // Registra explicitamente como Added quando um novo SubscriptionPayment é criado —
        // evita DbUpdateConcurrencyException (mesmo padrão de Catalog.AddModule/Learning.CompleteLesson).
        switch (payload.Event)
        {
            case "PAYMENT_RECEIVED":
            case "PAYMENT_CONFIRMED":
                var confirmedPayment = subscription.ConfirmPayment(payload.Payment.Id, payload.Payment.Value);
                if (confirmedPayment is not null)
                    await subscriptionRepository.AddPaymentAsync(confirmedPayment, ct);
                logger.LogInformation("Pagamento confirmado: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                var failedPayment = subscription.MarkPaymentFailed(payload.Payment.Id, payload.Payment.Value);
                if (failedPayment is not null)
                    await subscriptionRepository.AddPaymentAsync(failedPayment, ct);
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
