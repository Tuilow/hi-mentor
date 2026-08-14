using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Sales.Application.Commands.SimulateCourseSubscriptionPayment;

/// <summary>
/// Confirma manualmente uma assinatura de produto (ver SubscribeToCourseCommandHandler) que está
/// aguardando o webhook do Asaas — mesmo motivo/mesmo padrão de
/// SimulateCoursePurchasePaymentCommandHandler: em ambiente local o Asaas não alcança localhost.
///
/// Reaproveita exatamente o mesmo método de domínio usado pelo webhook real
/// (<see cref="Domain.Entities.Subscription.ConfirmPayment"/>), disparando o mesmo
/// PaymentConfirmedDomainEvent (matrícula automática em Learning) — nenhuma regra de negócio
/// nova. O AsaasPaymentId é sintético (não existe cobrança real no Asaas para confirmar) — só
/// precisa ser único por chamada, já que ConfirmPayment é idempotente por AsaasPaymentId. O
/// controller garante que este comando só é alcançável fora de Production.
/// </summary>
public sealed class SimulateCourseSubscriptionPaymentCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork uow,
    ILogger<SimulateCourseSubscriptionPaymentCommandHandler> logger
) : IRequestHandler<SimulateCourseSubscriptionPaymentCommand>
{
    public async Task Handle(SimulateCourseSubscriptionPaymentCommand request, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, ct)
            ?? throw new NotFoundException("Assinatura", request.SubscriptionId);

        if (request.UserId.HasValue && subscription.UserId != request.UserId.Value)
            throw new BusinessException("Esta assinatura não pertence a você.");

        var plan = await subscriptionRepository.GetPlanByIdAsync(subscription.PlanId, ct)
            ?? throw new NotFoundException("Plano", subscription.PlanId);

        var fakeAsaasPaymentId = $"sandbox-{Guid.NewGuid()}";
        var payment = subscription.ConfirmPayment(fakeAsaasPaymentId, plan.Price.Amount); // idempotente — mesma regra do webhook real
        if (payment is not null)
            await subscriptionRepository.AddPaymentAsync(payment, ct);

        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "[SANDBOX] Pagamento de assinatura simulado manualmente para a assinatura {SubscriptionId} (usuário {UserId}).",
            subscription.Id, subscription.UserId);
    }
}
