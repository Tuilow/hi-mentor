using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Commands.EnrollStudent;

public sealed class EnrollStudentCommandHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ICourseAccessChecker accessChecker,
    IUnitOfWork uow
) : IRequestHandler<EnrollStudentCommand, Guid>
{
    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (await enrollmentRepository.IsEnrolledAsync(request.UserId, request.CourseId, ct))
            throw new InvalidOperationException("Aluno já está matriculado neste curso.");

        // Verifica acesso: curso gratuito OU assinatura ativa (Sales)
        if (!course.IsFree)
        {
            var hasAccess = await accessChecker.HasActivePaidAccessAsync(request.UserId, ct);
            if (!hasAccess)
                throw new UnauthorizedException("Você precisa de uma assinatura ativa para acessar este curso.");
        }

        var enrollment = Enrollment.Create(request.UserId, request.CourseId, course.Title);
        await enrollmentRepository.AddAsync(enrollment, ct);
        await uow.SaveChangesAsync(ct);
        return enrollment.Id;
    }
}
