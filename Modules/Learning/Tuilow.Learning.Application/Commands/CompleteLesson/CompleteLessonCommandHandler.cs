using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Commands.CompleteLesson;

public sealed class CompleteLessonCommandHandler(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository,
    IUnitOfWork uow
) : IRequestHandler<CompleteLessonCommand>
{
    public async Task Handle(CompleteLessonCommand request, CancellationToken ct)
    {
        var enrollment = await enrollmentRepository.GetByIdAsync(request.EnrollmentId, ct)
            ?? throw new NotFoundException("Matrícula", request.EnrollmentId);

        if (enrollment.UserId != request.UserId)
            throw new ForbiddenException("Acesso negado a esta matrícula.");

        var course = await courseRepository.GetByIdAsync(enrollment.CourseId, ct)
            ?? throw new NotFoundException("Curso", enrollment.CourseId);

        var totalLessons = course.Modules.SelectMany(m => m.Lessons).Count();

        var newProgress = enrollment.TrackLessonProgress(
            request.LessonId, request.WatchedSeconds, request.TotalSeconds, totalLessons);

        // Registra explicitamente como Added quando é um progresso novo — evita
        // DbUpdateConcurrencyException (mesmo padrão usado em Catalog.AddModule/AddLesson).
        if (newProgress is not null)
            await enrollmentRepository.AddLessonProgressAsync(newProgress, ct);

        enrollmentRepository.Update(enrollment);
        await uow.SaveChangesAsync(ct);
    }
}
