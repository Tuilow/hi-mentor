using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;

namespace Tuilow.CreatorStudio.Domain.Interfaces;

public interface ILeadRepository : IRepository<Lead>
{
    Task<IEnumerable<Lead>> ListByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<int> CountByCourseAsync(Guid courseId, CancellationToken ct = default);
}
