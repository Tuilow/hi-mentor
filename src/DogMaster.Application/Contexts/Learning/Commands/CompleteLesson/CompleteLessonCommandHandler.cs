using DogMaster.Application.Common.Exceptions;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Catalog.Interfaces;
using DogMaster.Domain.Contexts.Learning.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Learning.Commands.CompleteLesson;

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
