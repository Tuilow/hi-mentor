using HiMentor.Learning.Domain.Entities;

namespace HiMentor.Learning.Domain.Interfaces;

/// <summary>
/// Repositório dedicado (não usa IRepository&lt;T&gt; genérico porque NotificationLog nunca é
/// atualizado/removido, só adicionado e consultado). Antes só era consultado por suporte via
/// SQL direto -- GetByCorrelationIdsAsync foi adicionado para alimentar a coluna "e-mail enviado"
/// da tela administrativa "Cursos e acessos" (ver GetUserCoursesAndAccessQueryHandler, módulo
/// IdentidadeAcesso), sem duplicar o dado em outra tabela.
/// </summary>
public interface INotificationLogRepository
{
    Task AddAsync(NotificationLog log, CancellationToken ct = default);

    /// <summary>Tentativas de notificação correlacionadas às compras/assinaturas informadas -- usado
    /// para mostrar status/data do último e-mail de acesso liberado por curso.</summary>
    Task<IReadOnlyList<NotificationLog>> GetByCorrelationIdsAsync(
        IEnumerable<Guid> correlationIds, CancellationToken ct = default);
}
