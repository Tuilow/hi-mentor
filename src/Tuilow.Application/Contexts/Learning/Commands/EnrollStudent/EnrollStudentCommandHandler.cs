using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using Tuilow.Domain.Contexts.Learning.Entities;
using Tuilow.Domain.Contexts.Learning.Interfaces;
using Tuilow.Domain.Contexts.Subscription.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Learning.Commands.EnrollStudent;

public sealed class EnrollStudentCommandHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
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

        // Verifica acesso: curso gratuito OU assinatura ativa OU curso comprado
        if (!course.IsFree)
        {
            var subscription = await subscriptionRepository.GetActiveByUserAsync(request.UserId, ct);
            if (subscription is null)
                throw new UnauthorizedException("Você precisa de uma assinatura ativa para acessar este curso.");
        }

        var enrollment = Enrollment.Create(request.UserId, request.CourseId, course.Title);
        await enrollmentRepository.AddAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);
        return enrollment.Id;
    }
}
