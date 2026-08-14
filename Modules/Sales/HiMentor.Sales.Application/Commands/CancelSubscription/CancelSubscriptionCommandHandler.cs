using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Sales.Application.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.Sales.Application.Commands.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IPaymentService paymentService,
    IUnitOfWork uow
) : IRequestHandler<CancelSubscriptionCommand>
{
    public async Task Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetActiveByUserAsync(request.UserId, ct)
            ?? throw new NotFoundException("Assinatura ativa", request.UserId);

        if (subscription.Status == SubscriptionStatus.Cancelled)
            return;

        if (subscription.AsaasSubscriptionId is not null)
            await paymentService.CancelSubscriptionAsync(subscription.AsaasSubscriptionId, ct);

        subscription.Cancel(request.Reason);
        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }
}
