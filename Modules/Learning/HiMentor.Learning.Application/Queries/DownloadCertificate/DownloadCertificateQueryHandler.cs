using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Learning.Application.Interfaces;
using HiMentor.Learning.Domain.Interfaces;
using MediatR;

namespace HiMentor.Learning.Application.Queries.DownloadCertificate;

public sealed class DownloadCertificateQueryHandler(
    ICertificateRepository certificateRepository,
    ICourseRepository courseRepository,
    IUserContactLookup userContactLookup,
    ICertificatePdfGenerator pdfGenerator
) : IRequestHandler<DownloadCertificateQuery, CertificatePdfResult?>
{
    public async Task<CertificatePdfResult?> Handle(DownloadCertificateQuery request, CancellationToken ct)
    {
        var certificate = await certificateRepository.GetByIdAsync(request.CertificateId, ct);
        // Certificado inexistente OU pertence a outro usuário — 404 nos dois casos (não 403),
        // pra não vazar pra um aluno que aquele CertificateId existe e é de outra pessoa.
        if (certificate is null || certificate.UserId != request.UserId) return null;

        var course = await courseRepository.GetByIdAsync(certificate.CourseId, ct);
        var courseTitle = course?.Title ?? "Curso removido";

        var contact = await userContactLookup.GetContactAsync(certificate.UserId, ct);
        var learnerName = contact is null ? "Aluno Hi Mentor" : $"{contact.FirstName} {contact.LastName}".Trim();

        var bytes = pdfGenerator.Generate(new CertificatePdfData(learnerName, courseTitle, certificate.Code, certificate.IssuedAt));
        var fileName = $"certificado-{Slugify(courseTitle)}.pdf";

        return new CertificatePdfResult(bytes, fileName);
    }

    /// <summary>
    /// Nome de arquivo seguro — troca só os caracteres que quebrariam um nome de arquivo
    /// (espaços, pontuação, barras) por "-"; letras acentuadas passam direto (IsLetterOrDigit
    /// considera "ã"/"ç" letras), o que é normal em nome de arquivo hoje em qualquer SO comum.
    /// Não precisa ser um slug "bonito", só válido.
    /// </summary>
    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        var chars = new char[normalized.Length];
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            chars[i] = char.IsLetterOrDigit(c) ? c : '-';
        }

        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');

        return string.IsNullOrEmpty(slug) ? "curso" : slug;
    }
}
