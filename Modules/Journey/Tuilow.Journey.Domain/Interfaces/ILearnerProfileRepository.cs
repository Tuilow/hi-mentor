using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Journey.Domain.Entities;

namespace Tuilow.Journey.Domain.Interfaces;

public interface ILearnerProfileRepository : IRepository<LearnerProfile>
{
    Task<IEnumerable<LearnerProfile>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
