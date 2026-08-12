using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Enums;
using Tuilow.Learning.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Learning.Infrastructure.Repositories;

/// <summary>
/// Recebe o DbContext genérico (não o concreto do Host) — mantém o módulo desacoplado.
/// </summary>
public sealed class EnrollmentRepository(DbContext context) : IEnrollmentRepository
{
    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<Enrollment>()
            .Include(e => e.LessonsProgress)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<Enrollment>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<Enrollment>().ToListAsync(ct);

    public async Task AddAsync(Enrollment entity, CancellationToken ct = default) =>
        await context.Set<Enrollment>().AddAsync(entity, ct);

    public void Update(Enrollment entity) => context.Set<Enrollment>().Update(entity);
    public void Delete(Enrollment entity) => context.Set<Enrollment>().Remove(entity);

    // Ordenado por EnrolledAt desc: um aluno reembolsado que compra o mesmo curso de novo gera uma
    // SEGUNDA linha de Enrollment (a primeira fica Cancelled — ver IsEnrolledAsync abaixo), então
    // sem essa ordenação esta consulta podia devolver a matrícula cancelada antiga em vez da atual.
    public async Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<Enrollment>()
            .Include(e => e.LessonsProgress)
            .Where(e => e.UserId == userId && e.CourseId == courseId)
            .OrderByDescending(e => e.EnrolledAt)
            .FirstOrDefaultAsync(ct);

    // Ignora matrículas Cancelled (achado 12/08/2026: um reembolso cancela o Enrollment via
    // CoursePurchaseRefundedEventHandler, mas esta consulta -- usada tanto para bloquear
    // matrícula duplicada em EnrollStudentCommandHandler quanto para decidir acesso em
    // LearningCourseAccessService.HasAccessAsync -- não filtrava por Status, então o aluno
    // continuava com acesso mesmo depois do Enrollment ser cancelado). Sem este filtro, cancelar
    // o Enrollment no reembolso não tinha efeito prático nenhum sobre o acesso.
    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<Enrollment>().AnyAsync(e =>
            e.UserId == userId && e.CourseId == courseId && e.Status != EnrollmentStatus.Cancelled, ct);

    public async Task<IEnumerable<Enrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Set<Enrollment>()
            .Include(e => e.LessonsProgress)
            .Where(e => e.UserId == userId)
            .ToListAsync(ct);

    /// <summary>
    /// Registra o LessonProgress explicitamente como Added no DbContext.
    /// Necessário porque DetectChanges marca entidades filhas com Guid novo como Modified.
    /// </summary>
    public async Task AddLessonProgressAsync(LessonProgress progress, CancellationToken ct = default) =>
        await context.Set<LessonProgress>().AddAsync(progress, ct);

    public async Task<int> CountByCourseAsync(Guid courseId, CancellationToken ct = default) =>
        await context.Set<Enrollment>().CountAsync(e => e.CourseId == courseId, ct);
}
