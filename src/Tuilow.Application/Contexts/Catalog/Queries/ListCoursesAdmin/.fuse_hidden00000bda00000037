using MediatR;

namespace DogMaster.Application.Contexts.Catalog.Queries.ListCoursesAdmin;

public sealed record ListCoursesAdminQuery : IRequest<IEnumerable<CourseAdminResponse>>;

public sealed record CourseAdminResponse(
    Guid Id,
    string Title,
    string Slug,
    string Level,
    string Status,
    decimal Price,
    bool IsFree,
    int ModuleCount,
    int LessonCount,
    DateTime CreatedAt,
    DateTime? PublishedAt
);
