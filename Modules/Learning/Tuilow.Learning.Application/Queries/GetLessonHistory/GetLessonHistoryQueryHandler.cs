using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Enums;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetLessonHistory;

/// <summary>
/// Acoplamento legítimo (mesmo padrão de GetMyEnrollments/GetContinueWatching): resolve título
/// do curso/aula no Catalog, já que LessonProgress só guarda LessonId. Usa
/// GetByIdsWithLessonsAsync (em vez do GetByIdsAsync "leve") porque aqui é preciso resolver o
/// título de cada aula, não só dados do curso.
/// </summary>
public sealed class GetLessonHistoryQueryHandler(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetLessonHistoryQuery, IEnumerable<LessonHistoryItemResponse>>
{
    public async Task<IEnumerable<LessonHistoryItemResponse>> Handle(GetLessonHistoryQuery request, CancellationToken ct)
    {
        var enrollments = (await enrollmentRepository.GetByUserAsync(request.UserId, ct))
            .Where(e => e.Status != EnrollmentStatus.Cancelled)
            .ToList();

        if (enrollments.Count == 0)
            return [];

        var courses = (await courseRepository.GetByIdsWithLessonsAsync(enrollments.Select(e => e.CourseId), ct))
            .ToDictionary(c => c.Id);

        var history = new List<LessonHistoryItemResponse>();

        foreach (var enrollment in enrollments)
        {
            if (!courses.TryGetValue(enrollment.CourseId, out var course))
                continue; // curso pode ter sido excluído — não quebra a listagem

            var lessonsById = course.Modules
                .SelectMany(m => m.Lessons)
                .ToDictionary(l => l.Id);

            foreach (var progress in enrollment.LessonsProgress)
            {
                if (!lessonsById.TryGetValue(progress.LessonId, out var lesson))
                    continue; // aula pode ter sido removida do curso

                history.Add(new LessonHistoryItemResponse(
                    course.Id, course.Title, course.Slug.Value, course.ThumbnailUrl,
                    lesson.Id, lesson.Title, progress.IsCompleted, progress.CompletedAt, progress.LastWatchedAt));
            }
        }

        return history.OrderByDescending(h => h.LastWatchedAt);
    }
}
