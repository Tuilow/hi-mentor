using Tuilow.Learning.Application.Queries.GetMyCertificates;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetCertificateForCourse;

/// <summary>
/// Feature 12/08/2026: "este curso específico já tem certificado emitido para o aluno logado?"
/// — usado pela tela de jornada (MentoradoJourneyView) para decidir se mostra o botão "Baixar
/// certificado" no bloco "Programa concluído" e para obter o CertificateId usado no download.
/// Retorna null (404) quando o curso ainda não foi concluído — normal, não é erro.
/// </summary>
public sealed record GetCertificateForCourseQuery(Guid UserId, Guid CourseId) : IRequest<MyCertificateResponse?>;
