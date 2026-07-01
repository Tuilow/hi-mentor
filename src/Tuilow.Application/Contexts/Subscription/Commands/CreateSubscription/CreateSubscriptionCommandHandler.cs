using Tuilow.Application.Common.Exceptions;
using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Subscription.Interfaces;
using MediatR;
using SubscriptionEntity = Tuilow.Domain.Contexts.Subscription.Entities.Subscription;

namespace Tuilow.Application.Contexts.Subscription.Commands.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
    ISubscriptionRepository subscriptionRepository,
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

        // Cria ou recupera customer no Asaas
        var customer = await paymentService.CreateOrGetCustomerAsync(
            new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

        // Cria assinatura no Asaas
        var asaasSubscription = await paymentService.CreateSubscriptionAsync(
            new(customer.Id, plan.AsaasPlanId!, plan.BillingCycle, plan.Price.Amount), ct);

        var subscription = SubscriptionEntity.Create(
            request.UserId, plan.Id, plan.BillingCycle,
            customer.Id, asaasSubscription.Id, plan.TrialDays);

        await subscriptionRepository.AddAsync(subscription, ct);
        await uow.SaveChangesAsync(ct);

        // Busca o link de pagamento do primeiro charge gerado pelo Asaas
        var paymentUrl = await paymentService.GetSubscriptionPaymentUrlAsync(asaasSubscription.Id, ct);

        return new CreateSubscriptionResponse(subscription.Id, asaasSubscription.Id, paymentUrl);
    }
}
