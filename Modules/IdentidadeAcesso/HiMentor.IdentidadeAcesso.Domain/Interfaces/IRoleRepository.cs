using HiMentor.IdentidadeAcesso.Domain.Entities;

namespace HiMentor.IdentidadeAcesso.Domain.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default);
}
