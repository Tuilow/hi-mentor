using HiMentor.Learning.Domain.Entities;
using HiMentor.Learning.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Learning.Infrastructure.Repositories;

/// <summary>Recebe o DbContext genérico (não o concreto do Host) -- mantém o módulo desacoplado.</summary>
public sealed class NotificationLogRepository(DbContext context) : INotificationLogRepository
{
    public async Task AddAsync(NotificationLog log, CancellationToken ct = default) =>
        await context.Set<NotificationLog>().AddAsync(log, ct);

    public async Task<IReadOnlyList<NotificationLog>> GetByCorrelationIdsAsync(
        IEnumerable<Guid> correlationIds, CancellationToken ct = default) =>
        await context.Set<NotificationLog>()
            .Where(n => n.CorrelationId != null && correlationIds.Contains(n.CorrelationId.Value))
            .ToListAsync(ct);
}
