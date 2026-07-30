using Tuilow.Learning.Domain.Entities;

namespace Tuilow.Learning.Domain.Interfaces;

/// <summary>
/// Repositório dedicado (não usa IRepository&lt;T&gt; genérico porque NotificationLog é
/// somente-escrita do ponto de vista do domínio — nunca é atualizado/removido, só consultado
/// por suporte via SQL/admin direto).
/// </summary>
public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog log, CancellationToken ct = default);
}
