using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.IdentidadeAcesso.Domain.Entities;

/// <summary>
/// Auditoria minima de acoes administrativas sobre acesso a curso -- painel do dono da
/// plataforma, secao "Cursos e acessos" dentro do detalhe de um usuario. Hoje so registra a
/// reemissao de link de acesso (a unica acao que gera uma credencial nova; a listagem de
/// cursos/pagamentos em si nao expoe token nenhum, entao nao precisa de auditoria). Mesmo
/// padrao de simplicidade de NotificationLog (Learning): entidade so-escrita do ponto de vista
/// do dominio, consultada por suporte/seguranca direto no banco quando necessario.
/// </summary>
public sealed class AdminCourseAccessAuditLog : Entity
{
    public Guid AdminUserId { get; private set; }
    public Guid StudentUserId { get; private set; }
    public Guid CourseId { get; private set; }
    public string Action { get; private set; } = string.Empty; // "ReissueAccessLink"

    private AdminCourseAccessAuditLog() { }

    public static AdminCourseAccessAuditLog Record(Guid adminUserId, Guid studentUserId, Guid courseId, string action) =>
        new()
        {
            AdminUserId = adminUserId,
            StudentUserId = studentUserId,
            CourseId = courseId,
            Action = action
        };
}
