using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Learning.Domain.Enums;
using HiMentor.Learning.Domain.Interfaces;
using MediatR;

namespace HiMentor.Learning.Application.Queries.GetContinueWatching;

/// <summary>
/// Acoplamento legítimo (mesmo padrão de GetMyEnrollments): busca o título/slug/thumbnail do
/// curso e o título da aula no Catalog, já que Enrollment/LessonProgress só guardam IDs.
/// </summary>
public sealed class GetContinueWatchingQueryHandler(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetContinueWatchingQuery, ContinueWatchingResponse?>
{
    public async Task<ContinueWatchingResponse?> Handle(GetContinueWatchingQuery request, CancellationToken ct)
    {
        var enrollments = (await enrollmentRepository.GetByUserAsync(request.UserId, ct))
            .Where(e => e.Status != EnrollmentStatus.Cancelled)
            .ToList();

        // Entre todas as aulas de todos os cursos matriculados, pega a última assistida —
        // "continuar de onde parei" não é por curso, é a última coisa que o aluno tocou.
        var last = enrollments
            .SelectMany(e => e.LessonsProgress.Select(lp => (Enrollment: e, Progress: lp)))
            .OrderByDescending(x => x.Progress.LastWatchedAt)
            .FirstOrDefault();

        if (last.Progress is null) return null;

        var course = await courseRepository.GetByIdAsync(last.Enrollment.CourseId, ct);
        if (course is null) return null;

        var lesson = course.Modules
            .SelectMany(m => m.Lessons)
            .FirstOrDefault(l => l.Id == last.Progress.LessonId);
        if (lesson is null) return null;

        return new ContinueWatchingResponse(
            course.Id, course.Title, course.Slug.Value, course.ThumbnailUrl,
            lesson.Id, lesson.Title, last.Enrollment.ProgressPercentage, last.Progress.LastWatchedAt);
    }
}
