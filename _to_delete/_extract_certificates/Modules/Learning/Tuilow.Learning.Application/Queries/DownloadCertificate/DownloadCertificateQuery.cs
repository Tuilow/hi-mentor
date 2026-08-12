using MediatR;

namespace Tuilow.Learning.Application.Queries.DownloadCertificate;

/// <summary>
/// Feature 12/08/2026: gera o PDF do certificado (ver ICertificatePdfGenerator) e devolve os
/// bytes prontos para download. UserId é o dono da sessão (nunca vem do client) — o handler
/// confere que o certificado pertence a este usuário antes de gerar qualquer coisa, então um
/// aluno não consegue baixar o certificado de outra pessoa só adivinhando um CertificateId.
/// </summary>
public sealed record DownloadCertificateQuery(Guid UserId, Guid CertificateId) : IRequest<CertificatePdfResult?>;

public sealed record CertificatePdfResult(byte[] Bytes, string FileName);
