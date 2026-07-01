using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using Tuilow.Domain.Contexts.Learning.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Learning.Commands.CompleteLesson;

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

        enrollment.TrackLessonProgress(
            request.LessonId, request.WatchedSeconds, request.TotalSeconds, totalLessons);

        enrollmentRepository.Update(enrollment);
        await uow.SaveChangesAsync(ct);
    }
}
