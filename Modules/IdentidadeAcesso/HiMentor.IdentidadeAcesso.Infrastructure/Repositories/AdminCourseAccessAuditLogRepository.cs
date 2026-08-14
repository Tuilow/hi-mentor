using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.IdentidadeAcesso.Infrastructure.Repositories;

/// <summary>Recebe o DbContext generico (nao o concreto do Host) -- mantem o modulo desacoplado.</summary>
public sealed class AdminCourseAccessAuditLogRepository(DbContext context) : IAdminCourseAccessAuditLogRepository
{
    public async Task AddAsync(AdminCourseAccessAuditLog log, CancellationToken ct = default) =>
        await context.Set<AdminCourseAccessAuditLog>().AddAsync(log, ct);
}
