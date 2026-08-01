using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using SubscriptionEntity = Tuilow.Sales.Domain.Entities.Subscription;

namespace Tuilow.Sales.Application.Commands.SubscribeToCourse;

public sealed class SubscribeToCourseCommandHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserProvisioningService userProvisioningService,
    IPaymentService paymentService,
    IUnitOfWork uow
) : IRequestHandler<SubscribeToCourseCommand, SubscribeToCourseResponse>
{
    public async Task<SubscribeToCourseResponse> Handle(SubscribeToCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        // Só existe no máximo um plano por produto (ver ISubscriptionRepository.GetPlansByCourseAsync).
        var plan = (await subscriptionRepository.GetPlansByCourseAsync(request.CourseId, ct))
            .FirstOrDefault(p => p.IsActive)
            ?? throw new BusinessException("Este curso não possui um plano de assinatura configurado.");

        // Checkout anônimo: sem login, localiza ou cria a conta pelo e-mail informado — mesmo
        // padrão de PurchaseCourseCommandHandler (compra avulsa).
        var userId = request.UserId
            ?? await userProvisioningService.FindOrCreateStudentAsync(request.CustomerEmail, request.CustomerName, ct);

        // Bug encontrado em teste manual: "subscriptions.UserId" tem FK real no Postgres pra
        // "users" (migration AddUserForeignKeys), mas de propósito nenhuma relação equivalente é
        // configurada no EF (ver comentário da migration — módulos não devem ganhar navegação
        // C# entre si). Sem isso, o EF não sabe que o User precisa ser inserido antes da
        // Subscription quando os dois são novos na mesma SaveChanges, e a ordem dos INSERTs não é
        // garantida — na prática, batia primeiro em "subscriptions" e violava a FK (23503). Um
        // SaveChanges aqui, logo após resolver/criar o usuário, garante a ordem sem precisar
        // configurar relação nenhuma no modelo. Não compromete a atomicidade na prática: se o
        // usuário já existia, isso é um no-op (nada pendente pra salvar); se era novo, a conta
        // fica criada mesmo que o pagamento falhe depois — mesmo resultado de alguém se
        // registrar e abandonar o checkout, sem dado inconsistente.
        await uow.SaveChangesAsync(ct);

        if (await subscriptionRepository.GetActiveByUserForCourseAsync(userId, request.CourseId, ct) is not null)
            throw new BusinessException("Você já tem uma assinatura ativa para este curso.");

        var customer = await paymentService.CreateOrGetCustomerAsync(
            new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

        // Observação: AsaasSubscriptionRequest.PlanId (abaixo, plan.AsaasPlanId) não é usado pelo
        // payload HTTP real enviado ao Asaas (ver AsaasPaymentService.CreateSubscriptionAsync) —
        // a assinatura recorrente é criada diretamente com valor/ciclo, sem depender de um plano
        // pré-cadastrado no lado do Asaas. Por isso é seguro chamar mesmo com Plan.AsaasPlanId
        // nulo (nunca é preenchido hoje — ver Plan.SetAsaasPlanId, não invocado em lugar nenhum).
        var asaasSubscription = await paymentService.CreateSubscriptionAsync(
            new(customer.Id, plan.AsaasPlanId ?? string.Empty, plan.BillingCycle, plan.Price.Amount), ct);

        var subscription = SubscriptionEntity.Create(
            userId, plan.Id, plan.BillingCycle, customer.Id, asaasSubscription.Id, plan.TrialDays);

        await subscriptionRepository.AddAsync(subscription, ct);
        await uow.SaveChangesAsync(ct);

        var paymentUrl = await paymentService.GetSubscriptionPaymentUrlAsync(asaasSubscription.Id, ct);

        return new SubscribeToCourseResponse(subscription.Id, asaasSubscription.Id, paymentUrl);
    }
}
