using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Learning.Application.Interfaces;
using HiMentor.Learning.Domain.Entities;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.Learning.Application.Commands.EnrollStudent;

public sealed class EnrollStudentCommandHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ICourseAccessChecker accessChecker,
    ISubscriptionRepository subscriptionRepository,
    IUnitOfWork uow
) : IRequestHandler<EnrollStudentCommand, Guid>
{
    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (await enrollmentRepository.IsEnrolledAsync(request.UserId, request.CourseId, ct))
            throw new InvalidOperationException("Aluno já está matriculado neste curso.");

        // Curso REALMENTE gratuito = Course.IsFree E sem nenhum Plan de assinatura ativo. Um
        // curso no modo "Assinatura" (passo Preço do assistente) grava Course.Price = 0 por
        // design — ver Course.SetPrice —, então checar só "course.IsFree" deixava qualquer
        // pessoa se matricular de graça em um curso de assinatura, pulando o pagamento por
        // completo (o mesmo dado que causava o curso aparecer como "Grátis" na tela também
        // liberava o acesso de graça aqui — ver CourseCommercializationResolver).
        var hasActiveSubscriptionPlan = (await subscriptionRepository.GetPlansByCourseAsync(request.CourseId, ct))
            .Any(p => p.IsActive);
        var isActuallyFree = course.IsFree && !hasActiveSubscriptionPlan;

        // Verifica acesso: curso realmente gratuito OU compra confirmada deste curso específico
        // (ou assinatura ativa — ver SalesCourseAccessChecker).
        if (!isActuallyFree)
        {
            var hasAccess = await accessChecker.HasActivePaidAccessAsync(request.UserId, request.CourseId, ct);
            if (!hasAccess)
                throw new UnauthorizedException("Você precisa comprar este curso para acessá-lo.");
        }

        var enrollment = Enrollment.Create(request.UserId, request.CourseId, course.Title);
        await enrollmentRepository.AddAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);
        return enrollment.Id;
    }
}
