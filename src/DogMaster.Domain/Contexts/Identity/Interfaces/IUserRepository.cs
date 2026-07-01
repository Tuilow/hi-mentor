using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Entities;

namespace DogMaster.Domain.Contexts.Identity.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    // EF Core marca RefreshTokens novos como Modified (Guid não-default) quando detectados via
    // DetectChanges em navegações. Chamar este método força o tracking como Added.
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    // Mesmo motivo: SocialLogin com Guid.NewGuid() seria marcado como Modified por DetectChanges.
    Task AddSocialLoginAsync(SocialLogin socialLogin, CancellationToken ct = default);
}
