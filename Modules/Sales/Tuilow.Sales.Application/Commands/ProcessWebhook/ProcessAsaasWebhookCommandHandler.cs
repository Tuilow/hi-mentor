using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Application.Commands.ProcessWebhook;

/// <summary>
/// Um único webhook do Asaas atende dois fluxos de pagamento do Sales:
///   - Assinatura da plataforma (Subscription/SubscriptionPayment) — modelo legado.
///   - Compra avulsa de curso (CoursePurchase) — modelo principal atual.
/// O evento chega sem indicar de qual fluxo se trata; o discriminador é a presença (ou não)
/// do campo "subscription" no payload — pagamentos avulsos nunca têm esse campo preenchido.
/// </summary>
public sealed class ProcessAsaasWebhookCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    IUnitOfWork uow,
    ILogger<ProcessAsaasWebhookCommandHandler> logger
) : IRequestHandler<ProcessAsaasWebhookCommand>
{
    public async Task Handle(ProcessAsaasWebhookCommand request, CancellationToken ct)
    {
        var payload = request.Payload;

        if (!string.IsNullOrEmpty(payload.Payment.Subscription))
        {
            await HandleSubscriptionPaymentAsync(payload, ct);
            return;
        }

        await HandleCoursePurchasePaymentAsync(payload, ct);
    }

    // ─── Assinatura da plataforma (modelo legado) ──────────────────────────────

    private async Task HandleSubscriptionPaymentAsync(AsaasWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await subscriptionRepository
            .GetByAsaasSubscriptionIdAsync(payload.Payment.Subscription!, ct);

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
                logger.LogInformation("Pagamento de assinatura confirmado: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                var failedPayment = subscription.MarkPaymentFailed(payload.Payment.Id, payload.Payment.Value);
                if (failedPayment is not null)
                    await subscriptionRepository.AddPaymentAsync(failedPayment, ct);
                logger.LogWarning("Pagamento de assinatura falhou: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_REFUNDED":
                // Faltava esse case — SubscriptionPayment.Refund() existia na entidade mas nunca
                // era chamado (achado M1 da auditoria). Revoga o acesso na hora (ver
                // Subscription.RefundPayment), diferente do cancelamento voluntário do aluno.
                subscription.RefundPayment(payload.Payment.Id);
                logger.LogInformation("Pagamento de assinatura reembolsado: {PaymentId}", payload.Payment.Id);
                break;

            default:
                logger.LogDebug("Evento Asaas ignorado (assinatura): {Event}", payload.Event);
                return;
        }

        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }

    // ─── Compra avulsa de curso (modelo principal) ─────────────────────────────

    private async Task HandleCoursePurchasePaymentAsync(AsaasWebhookPayload payload, CancellationToken ct)
    {
        var purchase = await coursePurchaseRepository.GetByAsaasPaymentIdAsync(payload.Payment.Id, ct);

        if (purchase is null)
        {
            logger.LogInformation("Webhook Asaas sem compra de curso correspondente: {PaymentId}", payload.Payment.Id);
            return;
        }

        switch (payload.Event)
        {
            case "PAYMENT_RECEIVED":
            case "PAYMENT_CONFIRMED":
                purchase.ConfirmPayment(); // idempotente — dispara CoursePurchaseConfirmedDomainEvent (Finance credita a carteira do criador)
                logger.LogInformation("Compra de curso confirmada: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                purchase.MarkFailed();
                logger.LogWarning("Compra de curso falhou: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_REFUNDED":
                purchase.Refund(); // dispara CoursePurchaseRefundedDomainEvent (Finance estorna a carteira do criador)
                logger.LogInformation("Compra de curso reembolsada: {PaymentId}", payload.Payment.Id);
                break;

            default:
                logger.LogDebug("Evento Asaas ignorado (compra de curso): {Event}", payload.Event);
                return;
        }

        coursePurchaseRepository.Update(purchase);
        await uow.SaveChangesAsync(ct);
    }
}
