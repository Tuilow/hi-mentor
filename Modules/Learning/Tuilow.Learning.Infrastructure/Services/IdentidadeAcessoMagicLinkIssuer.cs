using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;

namespace Tuilow.Learning.Infrastructure.Services;

/// <summary>Implementação real de <see cref="IMagicLinkIssuer"/> — grava no módulo IdentidadeAcesso.</summary>
public sealed class IdentidadeAcessoMagicLinkIssuer(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IMagicLinkIssuer
{
    public async Task<string?> IssueAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null) return null;

        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var magicLink = user.IssueMagicLink(token);

        // Mesmo padrão de AddRefreshTokenAsync — Guid não-default seria tratado como
        // Modified (UPDATE) por DetectChanges em vez de INSERT.
        await userRepository.AddMagicLinkTokenAsync(magicLink, ct);
        await uow.SaveChangesAsync(ct);

        return token;
    }
}
