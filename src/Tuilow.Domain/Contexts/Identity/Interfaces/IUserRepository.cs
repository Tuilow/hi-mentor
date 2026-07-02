using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Entities;

namespace Tuilow.Domain.Contexts.Identity.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task AddSocialLoginAsync(SocialLogin login, CancellationToken ct = default);
    Task AddUserRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken ct = default);
}
