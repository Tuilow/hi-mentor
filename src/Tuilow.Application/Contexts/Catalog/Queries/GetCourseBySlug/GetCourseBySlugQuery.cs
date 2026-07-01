using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Queries.GetCourseBySlug;

public sealed record GetCourseBySlugQuery(string Slug, Guid? CurrentUserId = null)
    : IRequest<CourseDetailResponse>;
