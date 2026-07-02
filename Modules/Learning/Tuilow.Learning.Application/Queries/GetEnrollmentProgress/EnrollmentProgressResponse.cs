namespace Tuilow.Learning.Application.Queries.GetEnrollmentProgress;

public sealed record EnrollmentProgressResponse(
    Guid EnrollmentId,
    Guid CourseId,
    string Status,
    decimal ProgressPercentage,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    IEnumerable<LessonProgressResponse> LessonProgress
);

public sealed record LessonProgressResponse(
    Guid LessonId,
    int WatchedSeconds,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime LastWatchedAt
);
