using HiMentor.SharedKernel.Application.Common;
using HiMentor.Catalog.Domain.Enums;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Queries.GetOtherCoursesByInstructor;

public sealed class GetOtherCoursesByInstructorQueryHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository
) : IRequestHandler<GetOtherCoursesByInstructorQuery, IEnumerable<InstructorCourseSummary>>
{
    public async Task<IEnumerable<InstructorCourseSummary>> Handle(
        GetOtherCoursesByInstructorQuery request, CancellationToken ct)
    {
        var courses = await courseRepository.ListByInstructorAsync(request.InstructorId, ct);

        var published = courses
            .Where(c => c.Status == CourseStatus.Published && c.Id != request.ExcludeCourseId)
            .OrderByDescending(c => c.PublishedAt)
            .ToList();

        var result = new List<InstructorCourseSummary>();
        foreach (var c in published)
        {
            // Estado real de comercialização — ver CourseCommercializationResolver.
            var hasActivePlan = (await subscriptionRepository.GetPlansByCourseAsync(c.Id, ct)).Any(p => p.IsActive);
            var commercializationState = CourseCommercializationResolver.Resolve(true, c.IsFree, hasActivePlan);

            result.Add(new InstructorCourseSummary(
                c.Id, c.Title, c.Slug.Value, c.ThumbnailUrl, c.Price.Amount, c.IsFree, c.Level.ToString(),
                commercializationState.ToString()));
        }

        return result;
    }
}
