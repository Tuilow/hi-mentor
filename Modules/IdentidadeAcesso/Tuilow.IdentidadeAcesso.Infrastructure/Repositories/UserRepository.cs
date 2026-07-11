using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Tuilow.IdentidadeAcesso.Infrastructure.Repositories;

/// <summary>
/// Depende só de DbContext (não de um DbContext concreto do Host) para o módulo não
/// precisar referenciar o projeto de composição — o Host injeta seu DbContext concreto,
/// que já tem os DbSets deste módulo via ApplyConfigurationsFromAssembly.
/// </summary>
public sealed class UserRepository(DbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.SocialLogins)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .ToListAsync(ct);

    public async Task AddAsync(User entity, CancellationToken ct = default) =>
        await context.Set<User>().AddAsync(entity, ct);

    public void Update(User entity) => context.Set<User>().Update(entity);
    public void Delete(User entity) => context.Set<User>().Remove(entity);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.SocialLogins)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Set<User>().AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.SocialLogins)
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.SocialLogins.Any(
                s => s.Provider == provider && s.ExternalId == externalId), ct);

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(r => r.Token == token), ct);

    // Força tracking como EntityState.Added para RefreshTokens criados em memória.
    // EF Core marca entidades com Guid não-default encontradas via DetectChanges como Modified,
    // o que gera UPDATE em vez de INSERT → DbUpdateConcurrencyException.
    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        await context.Set<RefreshToken>().AddAsync(token, ct);

    // Mesmo padrão para SocialLogin — Guid.NewGuid() causaria Modified via DetectChanges.
    public async Task AddSocialLoginAsync(SocialLogin socialLogin, CancellationToken ct = default) =>
        await context.Set<SocialLogin>().AddAsync(socialLogin, ct);

    // Mesmo padrão para UserRoleAssignment — sem isso, atribuir um role a um usuário
    // já rastreado (ex.: PromoteUserCommand) gera UPDATE (0 linhas afetadas) em vez de INSERT.
    public async Task AddUserRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken ct = default) =>
        await context.Set<UserRoleAssignment>().AddAsync(assignment, ct);

    public async Task<User?> GetByMagicLinkTokenAsync(string token, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.MagicLinkTokens)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.MagicLinkTokens.Any(m => m.Token == token), ct);

    // Mesmo padrão de AddRefreshTokenAsync — Guid não-default via Guid.NewGuid() no
    // construtor da entidade seria tratado como Modified (UPDATE) por DetectChanges.
    public async Task AddMagicLinkTokenAsync(MagicLinkToken token, CancellationToken ct = default) =>
        await context.Set<MagicLinkToken>().AddAsync(token, ct);
}
