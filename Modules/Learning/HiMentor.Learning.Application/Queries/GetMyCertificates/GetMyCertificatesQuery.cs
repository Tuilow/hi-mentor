using MediatR;

namespace HiMentor.Learning.Application.Queries.GetMyCertificates;

/// <summary>
/// Feature 12/08/2026: alimenta a aba "Certificados" do sidebar (lista de todos os certificados
/// já emitidos para o aluno logado) — quando vazia, o front-end mostra "Nenhum certificado ainda".
/// </summary>
public sealed record GetMyCertificatesQuery(Guid UserId) : IRequest<IEnumerable<MyCertificateResponse>>;

/// <summary>
/// Reaproveitado por GetCertificateForCourseQuery (mesmo shape, um único certificado) — ver
/// aquela query para o caso "este curso específico já tem certificado emitido?".
/// </summary>
public sealed record MyCertificateResponse(
    Guid CertificateId,
    string Code,
    Guid CourseId,
    string CourseTitle,
    string? ThumbnailUrl,
    DateTime IssuedAt
);
