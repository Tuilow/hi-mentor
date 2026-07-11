using Tuilow.Learning.Domain.Entities;
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

    public async Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<Enrollment>()
            .Include(e => e.LessonsProgress)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Set<Enrollment>().AnyAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

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
