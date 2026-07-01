using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.DogProfile.Entities;

namespace DogMaster.Domain.Contexts.DogProfile.Interfaces;

public interface IDogRepository : IRepository<Dog>
{
    Task<IEnumerable<Dog>> GetByUserAsync(Guid userId, CancellationToken ct = default);
}
