using Tuilow.Channel.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;

namespace Tuilow.Channel.Infrastructure.Services;

/// <summary>Implementação real de <see cref="ICreatorProfileLookup"/> — consulta o módulo IdentidadeAcesso.</summary>
public sealed class IdentidadeAcessoCreatorProfileLookup(IUserRepository userRepository) : ICreatorProfileLookup
{
    public async Task<CreatorProfile?> GetProfileAsync(Guid creatorId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(creatorId, ct);
        return user is null
            ? null
            : new CreatorProfile(user.Profile.FullName, user.Profile.AvatarUrl, user.Profile.Bio);
    }
}
