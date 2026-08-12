namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Feature 12/08/2026: "Baixar certificado" gera o PDF sob demanda, a cada clique — Certificate
/// não guarda um arquivo em disco/blob (PdfUrl continua null, ver Certificate.SetPdfUrl, que
/// nunca chegou a ser chamado por ninguém). Como o conteúdo é 100% determinístico (nome do
/// aluno, título do curso, código, data), reprocessar em cada download é mais simples do que
/// manter um storage de arquivos só para isto, e evita PDF desatualizado se o título do curso
/// mudar depois da emissão.
/// </summary>
public interface ICertificatePdfGenerator
{
    byte[] Generate(CertificatePdfData data);
}

public sealed record CertificatePdfData(
    string LearnerName,
    string CourseTitle,
    string Code,
    DateTime IssuedAt
);
