using MediatR;

namespace HiMentor.Catalog.Application.Queries.GetCourseBySlug;

public sealed record GetCourseBySlugQuery(string Slug, Guid? CurrentUserId = null)
    : IRequest<CourseDetailResponse>;
