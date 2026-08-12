using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Queries.GetMyCertificates;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetCertificateForCourse;

public sealed class GetCertificateForCourseQueryHandler(
    ICertificateRepository certificateRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetCertificateForCourseQuery, MyCertificateResponse?>
{
    public async Task<MyCertificateResponse?> Handle(GetCertificateForCourseQuery request, CancellationToken ct)
    {
        var certificate = await certificateRepository.GetByUserAndCourseAsync(request.UserId, request.CourseId, ct);
        if (certificate is null) return null;

        var course = await courseRepository.GetByIdAsync(certificate.CourseId, ct);
        if (course is null) return null;

        return new MyCertificateResponse(
            certificate.Id, certificate.Code, certificate.CourseId, course.Title, course.ThumbnailUrl, certificate.IssuedAt);
    }
}
