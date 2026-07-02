using Tuilow.Domain.Contexts.Identity.Entities;

namespace Tuilow.Domain.Contexts.Identity.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
}
