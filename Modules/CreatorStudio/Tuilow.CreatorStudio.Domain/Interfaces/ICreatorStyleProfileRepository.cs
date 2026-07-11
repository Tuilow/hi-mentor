using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.CreatorStudio.Domain.Entities;

namespace Tuilow.CreatorStudio.Domain.Interfaces;

public interface ICreatorStyleProfileRepository : IRepository<CreatorStyleProfile>
{
    Task<CreatorStyleProfile?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);
}
