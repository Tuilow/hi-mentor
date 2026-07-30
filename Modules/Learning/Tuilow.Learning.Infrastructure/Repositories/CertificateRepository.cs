using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Learning.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado, mesmo padrão de EnrollmentRepository.</summary>
public sealed class CertificateRepository(DbContext context) : ICertificateRepository
{
    public async Task<bool> ExistsForUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<Certificate>().AnyAsync(c => c.UserId == userId && c.CourseId == courseId, ct);

    public async Task<Certificate?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        await context.Set<Certificate>().FirstOrDefaultAsync(c => c.Code == code, ct);

    public async Task AddAsync(Certificate certificate, CancellationToken ct = default) =>
        await context.Set<Certificate>().AddAsync(certificate, ct);
}
