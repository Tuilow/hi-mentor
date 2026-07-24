using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Domain.Entities;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Sales.Application.Commands.CreateCourseSubscriptionPlan;

public sealed class CreateCourseSubscriptionPlanCommandHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork uow
) : IRequestHandler<CreateCourseSubscriptionPlanCommand, Guid>
{
    public async Task<Guid> Handle(CreateCourseSubscriptionPlanCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode definir o preço deste produto.");

        // Um produto tem no máximo um plano de assinatura (ver ISubscriptionRepository.
        // GetPlansByCourseAsync). Reaproveita o plano existente em vez de desativá-lo e criar um
        // novo: o Slug é derivado do nome do produto (determinístico), então criar um novo plano
        // sempre gerava o MESMO Slug do plano anterior — que continuava na tabela mesmo desativado
        // (Deactivate() é soft, não apaga a linha) — e a inserção estourava IX_plans_Slug (23505)
        // toda vez que o criador reabria o passo de preço e salvava de novo.
        var existingPlans = await subscriptionRepository.GetPlansByCourseAsync(request.CourseId, ct);
        var plan = existingPlans.FirstOrDefault();

        if (plan is not null)
        {
            plan.UpdatePricing(request.Price, request.BillingCycle, request.TrialDays);
            if (!plan.IsActive) plan.Reactivate();
        }
        else
        {
            // Nome curto do produto + trecho do CourseId garante slug único mesmo com títulos repetidos.
            var planName = $"Assinatura - {course.Title} ({course.Id.ToString()[..8]})";
            plan = Plan.Create(planName, request.Price, request.BillingCycle, request.TrialDays, request.CourseId);
            plan.SetDescription($"Assinatura recorrente do produto \"{course.Title}\".");
            await subscriptionRepository.AddPlanAsync(plan, ct);
        }

        await uow.SaveChangesAsync(ct);

        return plan.Id;
    }
}
