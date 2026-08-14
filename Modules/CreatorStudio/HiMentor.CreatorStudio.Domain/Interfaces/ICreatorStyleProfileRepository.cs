using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;

namespace HiMentor.CreatorStudio.Domain.Interfaces;

public interface ICreatorStyleProfileRepository : IRepository<CreatorStyleProfile>
{
    Task<CreatorStyleProfile?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default);
}
