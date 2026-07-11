using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Entities;

namespace Tuilow.IdentidadeAcesso.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task AddSocialLoginAsync(SocialLogin login, CancellationToken ct = default);
    Task AddUserRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken ct = default);
    Task<User?> GetByMagicLinkTokenAsync(string token, CancellationToken ct = default);
    Task AddMagicLinkTokenAsync(MagicLinkToken token, CancellationToken ct = default);
}
