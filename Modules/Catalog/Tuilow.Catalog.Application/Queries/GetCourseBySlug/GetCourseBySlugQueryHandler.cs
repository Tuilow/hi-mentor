using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetCourseBySlug;

public sealed class GetCourseBySlugQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseBySlugQuery, CourseDetailResponse>
{
    public async Task<CourseDetailResponse> Handle(GetCourseBySlugQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetBySlugAsync(request.Slug, ct)
            ?? throw new NotFoundException("Curso", request.Slug);

        var modules = course.Modules
            .OrderBy(m => m.Order)
            .Select(m => new ModuleResponse(
                m.Id, m.Title, m.Description, m.Order,
                m.Lessons.OrderBy(l => l.Order).Select(l => new LessonResponse(
                    l.Id, l.Title, l.Description, l.Order,
                    l.DurationSeconds, l.IsPreview, l.VideoId.HasValue))));

        var faqItems = course.FaqItems
            .OrderBy(f => f.Order)
            .Select(f => new FaqItemResponse(f.Id, f.Question, f.Answer, f.Order));

        return new CourseDetailResponse(
            course.Id, course.Title, course.Slug.Value, course.Description,
            course.ShortDescription, course.ThumbnailUrl, course.Price.Amount, course.IsFree,
            course.Level.ToString(), course.TotalDurationMinutes, course.PublishedAt, modules,
            course.Status.ToString(), course.Category, course.Subcategory, course.ProductType.ToString(),
            course.ViewCount, course.SalesPageHeadline, course.SalesPageSubheadline, course.SalesPageCtaText,
            course.SalesPageBenefits, faqItems);
    }
}
