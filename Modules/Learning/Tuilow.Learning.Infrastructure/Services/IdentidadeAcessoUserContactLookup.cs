using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;

namespace Tuilow.Learning.Infrastructure.Services;

/// <summary>Implementação real de <see cref="IUserContactLookup"/> — consulta o módulo IdentidadeAcesso.</summary>
public sealed class IdentidadeAcessoUserContactLookup(IUserRepository userRepository) : IUserContactLookup
{
    public async Task<UserContact?> GetContactAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        return user is null ? null : new UserContact(user.Email.Value, user.Profile.FirstName, user.Profile.Phone);
    }
}
