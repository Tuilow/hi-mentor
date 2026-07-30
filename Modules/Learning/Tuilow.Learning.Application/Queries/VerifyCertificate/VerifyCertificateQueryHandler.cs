using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.VerifyCertificate;

public sealed class VerifyCertificateQueryHandler(
    ICertificateRepository certificateRepository,
    ICourseRepository courseRepository,
    IUserContactLookup userContactLookup
) : IRequestHandler<VerifyCertificateQuery, CertificateVerificationResult?>
{
    public async Task<CertificateVerificationResult?> Handle(VerifyCertificateQuery request, CancellationToken ct)
    {
        var certificate = await certificateRepository.GetByCodeAsync(request.Code, ct);
        if (certificate is null) return null;

        var course = await courseRepository.GetByIdAsync(certificate.CourseId, ct);
        var contact = await userContactLookup.GetContactAsync(certificate.UserId, ct);
        var learnerName = contact is null ? "Aluno Tuilow" : $"{contact.FirstName} {contact.LastName}".Trim();

        return new CertificateVerificationResult(
            certificate.Code, learnerName, course?.Title ?? "Curso removido", certificate.IssuedAt);
    }
}
