using DogMaster.Application.Common.Exceptions;
using DogMaster.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Catalog.Queries.GetCourseBySlug;

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

        return new CourseDetailResponse(
            course.Id, course.Title, course.Slug.Value, course.Description,
            course.ShortDescription, course.ThumbnailUrl, course.Price.Amount, course.IsFree,
            course.Level.ToString(), course.TotalDurationMinutes, course.PublishedAt, modules);
    }
}
