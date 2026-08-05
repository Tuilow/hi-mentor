using Tuilow.IdentidadeAcesso.Domain.Entities;

namespace Tuilow.IdentidadeAcesso.Domain.Interfaces;

/// <summary>So-escrita do ponto de vista do dominio (mesmo padrao de INotificationLogRepository, em
/// Learning) -- consulta e feita por suporte/seguranca direto no banco.</summary>
public interface IAdminCourseAccessAuditLogRepository
{
    Task AddAsync(AdminCourseAccessAuditLog log, CancellationToken ct = default);
}
