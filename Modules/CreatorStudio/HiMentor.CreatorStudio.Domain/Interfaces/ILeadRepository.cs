using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;

namespace HiMentor.CreatorStudio.Domain.Interfaces;

public interface ILeadRepository : IRepository<Lead>
{
    Task<IEnumerable<Lead>> ListByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<int> CountByCourseAsync(Guid courseId, CancellationToken ct = default);
}
