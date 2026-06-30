using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Entities;

namespace DogMaster.Domain.Contexts.Identity.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
}
