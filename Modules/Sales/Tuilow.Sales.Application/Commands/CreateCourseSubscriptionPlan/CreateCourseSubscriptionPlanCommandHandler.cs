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

        // Desativa planos de assinatura antigos deste produto (assinantes já ativos continuam
        // no plano antigo até cancelar — Deactivate() só impede NOVAS assinaturas nele).
        // Os planos vêm rastreados pelo EF (mesma query/DbContext) — só mutar já basta, o
        // DetectChanges do SaveChangesAsync marca como Modified automaticamente.
        var existingPlans = await subscriptionRepository.GetPlansByCourseAsync(request.CourseId, ct);
        foreach (var oldPlan in existingPlans.Where(p => p.IsActive))
            oldPlan.Deactivate();

        // Nome curto do produto + trecho do CourseId garante slug único mesmo com títulos repetidos.
        var planName = $"Assinatura - {course.Title} ({course.Id.ToString()[..8]})";
        var plan = Plan.Create(planName, request.Price, request.BillingCycle, request.TrialDays, request.CourseId);
        plan.SetDescription($"Assinatura recorrente do produto \"{course.Title}\".");

        await subscriptionRepository.AddPlanAsync(plan, ct);
        await uow.SaveChangesAsync(ct);

        return plan.Id;
    }
}
