namespace Tuilow.Application.Contexts.Catalog.Queries.GetCourseBySlug;

public sealed record CourseDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string? ShortDescription,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level,
    int TotalDurationMinutes,
    DateTime? PublishedAt,
    IEnumerable<ModuleResponse> Modules
);

public sealed record ModuleResponse(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    IEnumerable<LessonResponse> Lessons
);

public sealed record LessonResponse(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    int? DurationSeconds,
    bool IsPreview,
    bool HasVideo
);
