namespace Tuilow.Application.Contexts.Catalog.Queries.ListCourses;

public sealed record CourseListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string? ShortDescription,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level,
    int TotalDurationMinutes,
    DateTime? PublishedAt
);
