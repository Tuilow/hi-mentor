using HiMentor.SharedKernel.Application.Common;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Queries.ListCourses;

public sealed class ListCoursesQueryHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository
) : IRequestHandler<ListCoursesQuery, PagedList<CourseListItemResponse>>
{
    public async Task<PagedList<CourseListItemResponse>> Handle(ListCoursesQuery request, CancellationToken ct)
    {
        var (courses, total) = await courseRepository.ListPublishedAsync(
            request.Level, request.Search, request.Page, request.PageSize, ct);

        var items = new List<CourseListItemResponse>();
        foreach (var c in courses)
        {
            // Estado real de comercialização — ver CourseCommercializationResolver. ListPublishedAsync
            // já filtra só cursos publicados, por isso isPublished é sempre true aqui.
            var hasActivePlan = (await subscriptionRepository.GetPlansByCourseAsync(c.Id, ct)).Any(p => p.IsActive);
            var commercializationState = CourseCommercializationResolver.Resolve(true, c.IsFree, hasActivePlan);

            items.Add(new CourseListItemResponse(
                c.Id, c.Title, c.Slug.Value, c.ShortDescription, c.ThumbnailUrl,
                c.Price.Amount, c.IsFree, c.Level.ToString(),
                c.TotalDurationMinutes, c.PublishedAt, c.Category, c.ProductType.ToString(),
                commercializationState.ToString()));
        }

        return new PagedList<CourseListItemResponse>(items, total, request.Page, request.PageSize);
    }
}
