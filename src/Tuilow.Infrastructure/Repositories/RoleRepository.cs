using Tuilow.Domain.Contexts.Identity.Entities;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using Tuilow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.Infrastructure.Repositories;

public sealed class RoleRepository(ApplicationDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await context.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct = default) =>
        await context.Roles.ToListAsync(ct);
}
