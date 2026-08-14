using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Journey.Domain.Entities;

namespace HiMentor.Journey.Domain.Interfaces;

public interface ILearnerProfileRepository : IRepository<LearnerProfile>
{
    Task<IEnumerable<LearnerProfile>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
