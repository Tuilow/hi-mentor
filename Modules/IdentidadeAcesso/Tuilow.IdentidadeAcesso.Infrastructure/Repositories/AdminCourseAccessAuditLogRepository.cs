using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.IdentidadeAcesso.Infrastructure.Repositories;

/// <summary>Recebe o DbContext generico (nao o concreto do Host) -- mantem o modulo desacoplado.</summary>
public sealed class AdminCourseAccessAuditLogRepository(DbContext context) : IAdminCourseAccessAuditLogRepository
{
    public async Task AddAsync(AdminCourseAccessAuditLog log, CancellationToken ct = default) =>
        await context.Set<AdminCourseAccessAuditLog>().AddAsync(log, ct);
}
