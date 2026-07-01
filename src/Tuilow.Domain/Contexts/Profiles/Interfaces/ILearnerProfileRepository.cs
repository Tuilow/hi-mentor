using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Profiles.Entities;

namespace Tuilow.Domain.Contexts.Profiles.Interfaces;

public interface ILearnerProfileRepository : IRepository<LearnerProfile>
{
    Task<IEnumerable<LearnerProfile>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
