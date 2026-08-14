using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Learning.Domain.Interfaces;
using MediatR;

namespace HiMentor.Learning.Application.Commands.CompleteLesson;

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
            request.LessonId, request.WatchedSeconds, request.TotalSeconds, totalLessons, request.ClientCapturedAt);

        // Registra explicitamente como Added quando é um progresso novo — evita
        // DbUpdateConcurrencyException (mesmo padrão usado em Catalog.AddModule/AddLesson).
        if (newProgress is not null)
            await enrollmentRepository.AddLessonProgressAsync(newProgress, ct);

        enrollmentRepository.Update(enrollment);
        await uow.SaveChangesAsync(ct);
    }
}
