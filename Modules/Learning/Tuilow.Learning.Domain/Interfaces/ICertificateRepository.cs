using Tuilow.Learning.Domain.Entities;

namespace Tuilow.Learning.Domain.Interfaces;

/// <summary>
/// Achado A4 da avaliação: Certificate existia no domínio (geração de código, tabela mapeada
/// via CertificateConfiguration) mas nunca era instanciado — nenhum repositório/handler chegava
/// a chamar Certificate.Issue(). Ver CourseCompletedEventHandler (Learning.Application), que
/// agora emite o certificado reagindo à conclusão do curso.
/// </summary>
public interface ICertificateRepository
{
    /// <summary>
    /// Idempotência: CourseCompletedDomainEvent não deveria disparar duas vezes para a mesma
    /// matrícula (Enrollment.Complete() tem guard), mas isto protege contra reprocessamento
    /// (ex.: retry manual, mesmo padrão dos outros handlers desta base) sem depender de uma
    /// constraint única no banco.
    /// </summary>
    Task<bool> ExistsForUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);

    /// <summary>Usado pela verificação pública de autenticidade (GET /certificates/verify/{code}).</summary>
    Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task AddAsync(Certificate certificate, CancellationToken ct = default);
}
