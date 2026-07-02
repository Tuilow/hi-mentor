using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Learning.Domain.Entities;

namespace Tuilow.Learning.Domain.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o LessonProgress — evita DbUpdateConcurrencyException.</summary>
    Task AddLessonProgressAsync(LessonProgress progress, CancellationToken ct = default);
}
