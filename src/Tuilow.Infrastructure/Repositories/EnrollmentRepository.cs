using Tuilow.Domain.Contexts.Learning.Entities;
using Tuilow.Domain.Contexts.Learning.Interfaces;
using Tuilow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Infrastructure.Repositories;

public sealed class EnrollmentRepository(ApplicationDbContext context) : IEnrollmentRepository
{
    public async Task<Enrollment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Enrollments
            .Include(e => e.LessonsProgress)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<Enrollment>> GetAllAsync(CancellationToken ct = default) =>
        await context.Enrollments.ToListAsync(ct);

    public async Task AddAsync(Enrollment entity, CancellationToken ct = default) =>
        await context.Enrollments.AddAsync(entity, ct);

    public void Update(Enrollment entity) => context.Enrollments.Update(entity);
    public void Delete(Enrollment entity) => context.Enrollments.Remove(entity);

    public async Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Enrollments
            .Include(e => e.LessonsProgress)
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

    public async Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default) =>
        await context.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId, ct);

    public async Task<IEnumerable<Enrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Enrollments
            .Include(e => e.LessonsProgress)
            .Where(e => e.UserId == userId)
            .ToListAsync(ct);
}
