using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetOtherCoursesByInstructor;

public sealed class GetOtherCoursesByInstructorQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetOtherCoursesByInstructorQuery, IEnumerable<InstructorCourseSummary>>
{
    public async Task<IEnumerable<InstructorCourseSummary>> Handle(
        GetOtherCoursesByInstructorQuery request, CancellationToken ct)
    {
        var courses = await courseRepository.ListByInstructorAsync(request.InstructorId, ct);

        return courses
            .Where(c => c.Status == CourseStatus.Published && c.Id != request.ExcludeCourseId)
            .OrderByDescending(c => c.PublishedAt)
            .Select(c => new InstructorCourseSummary(
                c.Id, c.Title, c.Slug.Value, c.ThumbnailUrl, c.Price.Amount, c.IsFree, c.Level.ToString()));
    }
}
