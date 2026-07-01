using Tuilow.Application.Common.Exceptions;
using Tuilow.Application.Contexts.Catalog.Queries.GetCourseBySlug;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Queries.GetCourseByIdAdmin;

public sealed class GetCourseByIdAdminQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdAdminQuery, CourseDetailResponse>
{
    public async Task<CourseDetailResponse> Handle(GetCourseByIdAdminQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

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
