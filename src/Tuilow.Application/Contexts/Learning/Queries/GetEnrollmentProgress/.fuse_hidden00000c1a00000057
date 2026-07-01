using DogMaster.Domain.Contexts.Learning.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Learning.Queries.GetEnrollmentProgress;

public sealed class GetEnrollmentProgressQueryHandler(IEnrollmentRepository enrollmentRepository)
    : IRequestHandler<GetEnrollmentProgressQuery, EnrollmentProgressResponse?>
{
    public async Task<EnrollmentProgressResponse?> Handle(GetEnrollmentProgressQuery request, CancellationToken ct)
    {
        var enrollment = await enrollmentRepository.GetByUserAndCourseAsync(request.UserId, request.CourseId, ct);
        if (enrollment is null) return null;

        var progress = enrollment.LessonsProgress.Select(p => new LessonProgressResponse(
            p.LessonId, p.WatchedSeconds, p.IsCompleted, p.CompletedAt, p.LastWatchedAt));

        return new EnrollmentProgressResponse(
            enrollment.Id, enrollment.CourseId, enrollment.Status.ToString(),
            enrollment.ProgressPercentage, enrollment.EnrolledAt, enrollment.CompletedAt, progress);
    }
}
