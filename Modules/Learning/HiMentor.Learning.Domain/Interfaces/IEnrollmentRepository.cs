using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Learning.Domain.Entities;

namespace HiMentor.Learning.Domain.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Total de alunos matriculados no curso — card "Students" do dashboard do produto.</summary>
    Task<int> CountByCourseAsync(Guid courseId, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o LessonProgress — evita DbUpdateConcurrencyException.</summary>
    Task AddLessonProgressAsync(LessonProgress progress, CancellationToken ct = default);
}
