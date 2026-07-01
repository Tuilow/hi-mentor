using Tuilow.Application.Common.Exceptions;
using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Subscription.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Subscription.Commands.CancelSubscription;

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

        if (subscription.AsaasSubscriptionId is not null)
            await paymentService.CancelSubscriptionAsync(subscription.AsaasSubscriptionId, ct);

        subscription.Cancel(request.Reason);
        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }
}
