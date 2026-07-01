using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Learning.Entities;

namespace Tuilow.Domain.Contexts.Learning.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetByUserAndCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
