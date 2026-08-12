using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetMyCertificates;

/// <summary>
/// Mesmo padrão de acoplamento legítimo de GetMyEnrollmentsQueryHandler: Certificate (Learning)
/// só guarda CourseId, então título/capa do curso vêm do Catalog daqui — não o contrário.
/// </summary>
public sealed class GetMyCertificatesQueryHandler(
    ICertificateRepository certificateRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetMyCertificatesQuery, IEnumerable<MyCertificateResponse>>
{
    public async Task<IEnumerable<MyCertificateResponse>> Handle(GetMyCertificatesQuery request, CancellationToken ct)
    {
        var certificates = await certificateRepository.GetByUserAsync(request.UserId, ct);
        if (certificates.Count == 0) return [];

        var courses = (await courseRepository.GetByIdsAsync(certificates.Select(c => c.CourseId), ct))
            .ToDictionary(c => c.Id);

        return certificates
            .Where(c => courses.ContainsKey(c.CourseId)) // curso pode ter sido excluído — não quebra a listagem
            .Select(c =>
            {
                var course = courses[c.CourseId];
                return new MyCertificateResponse(
                    c.Id, c.Code, c.CourseId, course.Title, course.ThumbnailUrl, c.IssuedAt);
            });
    }
}
