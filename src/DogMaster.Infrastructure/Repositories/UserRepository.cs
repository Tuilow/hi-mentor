using DogMaster.Domain.Contexts.Identity.Entities;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using DogMaster.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DogMaster.Infrastructure.Repositories;

public sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.SocialLogins)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken ct = default) =>
        await context.Users.Include(u => u.Profile).ToListAsync(ct);

    public async Task AddAsync(User entity, CancellationToken ct = default) =>
        await context.Users.AddAsync(entity, ct);

    public void Update(User entity) => context.Users.Update(entity);
    public void Delete(User entity) => context.Users.Remove(entity);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.SocialLogins)
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Users.AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Profile)
            .Include(u => u.SocialLogins)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.SocialLogins.Any(
                s => s.Provider == provider && s.ExternalId == externalId), ct);

    public async Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default) =>
        await context.Users
            .Include(u => u.Profile)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(r => r.Token == token), ct);

    // Força tracking como EntityState.Added para RefreshTokens criados em memória.
    // EF Core marca entidades com Guid não-default encontradas via DetectChanges como Modified,
    // o que gera UPDATE em vez de INSERT → DbUpdateConcurrencyException.
    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default) =>
        await context.RefreshTokens.AddAsync(token, ct);

    // Mesmo padrão para SocialLogin — Guid.NewGuid() causaria Modified via DetectChanges.
    public async Task AddSocialLoginAsync(SocialLogin socialLogin, CancellationToken ct = default) =>
        await context.SocialLogins.AddAsync(socialLogin, ct);
}
