using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.IdentidadeAcesso.Infrastructure.Repositories;

public sealed class RoleRepository(DbContext context) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await context.Set<Role>().FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default) =>
        await context.Set<Role>().ToListAsync(ct);
}
