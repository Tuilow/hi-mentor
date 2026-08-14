using MediatR;

namespace HiMentor.Learning.Application.Queries.VerifyCertificate;

/// <summary>
/// Achado A4 da avaliação: endpoint público de verificação de autenticidade — qualquer pessoa
/// com o código (ex.: um recrutador conferindo um certificado citado num currículo) pode
/// confirmar que ele foi realmente emitido, sem precisar de login.
/// </summary>
public sealed record VerifyCertificateQuery(string Code) : IRequest<CertificateVerificationResult?>;

public sealed record CertificateVerificationResult(
    string Code, string LearnerName, string CourseTitle, DateTime IssuedAt);
