using Tuilow.Finance.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;

namespace Tuilow.Finance.Infrastructure.Services;

/// <summary>Implementação real de <see cref="ICreatorDisplayInfoLookup"/> — consulta o módulo IdentidadeAcesso.</summary>
public sealed class IdentidadeAcessoCreatorDisplayInfoLookup(IUserRepository userRepository) : ICreatorDisplayInfoLookup
{
    public async Task<IReadOnlyDictionary<Guid, CreatorDisplayInfo>> GetManyAsync(
        IEnumerable<Guid> creatorIds, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, CreatorDisplayInfo>();

        // Lista pequena e paginada (painel admin) -- busca individual é suficiente, sem
        // necessidade de um método de repositório novo "GetManyByIdsAsync" só para isto.
        foreach (var id in creatorIds.Distinct())
        {
            var user = await userRepository.GetByIdAsync(id, ct);
            if (user is not null)
                result[id] = new CreatorDisplayInfo(user.Profile.FullName, user.Email.Value);
        }

        return result;
    }
}
