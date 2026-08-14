using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.IdentidadeAcesso.Infrastructure.Repositories;

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

    // PasswordResetToken é uma coluna simples do próprio User (não uma coleção filha como
    // MagicLinkTokens), então a busca é direta pelo campo — mesmo padrão de GetByEmailAsync.
    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default) =>
        await context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, ct);

    /// <summary>
    /// Listagem paginada do painel do dono da plataforma. Inclui RefreshTokens porque a query
    /// handler (camada de aplicação) calcula o "último login" a partir do CreatedAt mais recente
    /// — mais simples do que expor essa lógica aqui no repositório.
    /// </summary>
    public async Task<(IEnumerable<User> Items, int Total)> ListAllAsync(
        string? search, string? roleFilter, UserStatus? statusFilter,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Set<User>()
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoleAssignments).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.Email.Value, pattern) ||
                EF.Functions.ILike(u.Profile.FirstName, pattern) ||
                EF.Functions.ILike(u.Profile.LastName, pattern));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
            query = query.Where(u => u.UserRoleAssignments.Any(ur => ur.Role.Name == roleFilter));

        if (statusFilter.HasValue)
            query = query.Where(u => u.Status == statusFilter.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>Contagens agregadas direto no banco (COUNT), sem materializar as entidades — usado na visão geral do painel do dono.</summary>
    public async Task<UserCountsSnapshot> GetCountsSnapshotAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cutoff24h = now.AddHours(-24);
        var cutoff7d = now.AddDays(-7);

        var totalUsers = await context.Set<User>().CountAsync(ct);
        var activeUsers = await context.Set<User>().CountAsync(u => u.Status == UserStatus.Active, ct);
        var suspendedUsers = await context.Set<User>().CountAsync(u => u.Status == UserStatus.Suspended, ct);
        var totalCreators = await context.Set<User>()
            .CountAsync(u => u.UserRoleAssignments.Any(ur => ur.Role.Name == RoleNames.Creator), ct);
        var activeLast24h = await context.Set<User>()
            .CountAsync(u => u.RefreshTokens.Any(t => t.CreatedAt >= cutoff24h), ct);
        var activeLast7d = await context.Set<User>()
            .CountAsync(u => u.RefreshTokens.Any(t => t.CreatedAt >= cutoff7d), ct);

        return new UserCountsSnapshot(totalUsers, activeUsers, suspendedUsers, totalCreators, activeLast24h, activeLast7d);
    }
}
