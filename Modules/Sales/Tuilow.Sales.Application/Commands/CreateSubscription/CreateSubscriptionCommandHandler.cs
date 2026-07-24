using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using SubscriptionEntity = Tuilow.Sales.Domain.Entities.Subscription;

namespace Tuilow.Sales.Application.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    IUserProvisioningService userProvisioningService,
    IPaymentService paymentService,
    IUnitOfWork uow
) : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResponse>
{
    public async Task<CreateSubscriptionResponse> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var plan = await subscriptionRepository.GetPlanByIdAsync(request.PlanId, ct)
            ?? throw new NotFoundException("Plano", request.PlanId);

        if (!plan.IsActive)
            throw new InvalidOperationException("Este plano não está mais disponível.");

        // Checkout anônimo: mesmo modelo da compra avulsa de curso.
        var userId = request.UserId
            ?? await userProvisioningService.FindOrCreateStudentAsync(
                request.CustomerEmail, request.CustomerName, ct);

        var customer = await paymentService.CreateOrGetCustomerAsync(
            new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

        var asaasSubscription = await paymentService.CreateSubscriptionAsync(
            new(customer.Id, plan.AsaasPlanId!, plan.BillingCycle, plan.Price.Amount), ct);

        var subscription = SubscriptionEntity.Create(
            userId, plan.Id, plan.BillingCycle,
            customer.Id, asaasSubscription.Id, plan.TrialDays);

        await subscriptionRepository.AddAsync(subscription, ct);
        await uow.SaveChangesAsync(ct);

        var paymentUrl = await paymentService.GetSubscriptionPaymentUrlAsync(asaasSubscription.Id, ct);

        return new CreateSubscriptionResponse(subscription.Id, asaasSubscription.Id, paymentUrl);
    }
}
