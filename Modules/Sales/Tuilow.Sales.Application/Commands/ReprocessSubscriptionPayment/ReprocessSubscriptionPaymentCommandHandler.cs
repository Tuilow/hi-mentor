using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Sales.Application.Commands.ReprocessCoursePurchase;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Events;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Application.Commands.ReprocessSubscriptionPayment;

public sealed class ReprocessSubscriptionPaymentCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IEnumerable<INotificationHandler<PaymentConfirmedDomainEvent>> handlers,
    ILogger<ReprocessSubscriptionPaymentCommandHandler> logger
) : IRequestHandler<ReprocessSubscriptionPaymentCommand, ReprocessResult>
{
    public async Task<ReprocessResult> Handle(ReprocessSubscriptionPaymentCommand request, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, ct)
            ?? throw new NotFoundException("Assinatura", request.SubscriptionId);

        var payment = subscription.Payments.SingleOrDefault(p => p.AsaasPaymentId == request.AsaasPaymentId);
        if (payment is null)
            return new ReprocessResult(false, $"Nenhum pagamento com AsaasPaymentId {request.AsaasPaymentId} encontrado nesta assinatura.");

        if (payment.Status != PaymentStatus.Confirmed)
            return new ReprocessResult(false, $"Pagamento esta com status {payment.Status} (nao Confirmed) - nada a reprocessar.");

        var domainEvent = new PaymentConfirmedDomainEvent(
            subscription.Id, subscription.UserId, payment.AsaasPaymentId, payment.Amount.Amount);

        var failures = new List<string>();
        var handlerCount = 0;

        foreach (var handler in handlers)
        {
            handlerCount++;
            try
            {
                await handler.Handle(domainEvent, ct);
            }
            catch (Exception ex)
            {
                failures.Add($"{handler.GetType().Name}: {ex.Message}");
                logger.LogError(ex,
                    "Falha no handler {HandlerType} ao reprocessar manualmente o pagamento {AsaasPaymentId} da assinatura {SubscriptionId}.",
                    handler.GetType().Name, payment.AsaasPaymentId, subscription.Id);
            }
        }

        if (failures.Count > 0)
        {
            return new ReprocessResult(
                false,
                $"Reprocessamento parcial: {handlerCount - failures.Count} handler(s) concluido(s), {failures.Count} falharam. {string.Join(" | ", failures)}");
        }

        logger.LogInformation(
            "Reprocessamento manual do pagamento {AsaasPaymentId} da assinatura {SubscriptionId} concluido.",
            payment.AsaasPaymentId, subscription.Id);
        return new ReprocessResult(true, "Reprocessado com sucesso.");
    }
}
