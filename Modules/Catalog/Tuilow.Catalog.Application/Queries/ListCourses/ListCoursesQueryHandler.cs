using Tuilow.SharedKernel.Application.Common;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.ListCourses;

public sealed class ListCoursesQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<ListCoursesQuery, PagedList<CourseListItemResponse>>
{
    public async Task<PagedList<CourseListItemResponse>> Handle(ListCoursesQuery request, CancellationToken ct)
    {
        var (courses, total) = await courseRepository.ListPublishedAsync(
            request.Level, request.Search, request.Page, request.PageSize, ct);

        var items = courses.Select(c => new CourseListItemResponse(
            c.Id, c.Title, c.Slug.Value, c.ShortDescription, c.ThumbnailUrl,
            c.Price.Amount, c.IsFree, c.Level.ToString(),
            c.TotalDurationMinutes, c.PublishedAt, c.Category, c.ProductType.ToString()));

        return new PagedList<CourseListItemResponse>(items, total, request.Page, request.PageSize);
    }
}
