using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetAccessCodesAdmin;

public sealed class GetAccessCodesAdminQueryHandler(
    IAccessCodeRepository accessCodeRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetAccessCodesAdminQuery, IReadOnlyList<AccessCodeAdminResponse>>
{
    public async Task<IReadOnlyList<AccessCodeAdminResponse>> Handle(GetAccessCodesAdminQuery request, CancellationToken ct)
    {
        var codes = (await accessCodeRepository.GetAllAdminAsync(ct)).ToList();
        if (codes.Count == 0) return [];

        // Mesmo padrão de GetUserCoursesAndAccessQueryHandler: batch-fetch por CourseId em vez de
        // uma query por código (evita N+1 quando a lista crescer).
        var courseIds = codes.Select(c => c.CourseId).Distinct().ToList();
        var courses = (await courseRepository.GetByIdsAsync(courseIds, ct)).ToDictionary(c => c.Id);

        return codes.Select(c => new AccessCodeAdminResponse(
            c.Id,
            c.Code,
            c.CourseId,
            courses.TryGetValue(c.CourseId, out var course) ? course.Title : "(curso removido)",
            c.MaxUses,
            c.UsesCount,
            c.ExpiresAt,
            c.IsActive,
            c.CreatedAt
        )).ToList();
    }
}
