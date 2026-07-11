namespace Tuilow.Channel.Application.Interfaces;

/// <summary>Dados públicos mínimos do criador — nome, avatar, bio — exibidos no Canal.</summary>
public sealed record CreatorProfile(string DisplayName, string? AvatarUrl, string? Bio);

/// <summary>
/// Porta (anti-corruption layer) que abstrai "quem é este criador?" sem o módulo Channel
/// depender diretamente do domínio de IdentidadeAcesso — mesmo padrão de
/// Catalog.Application.Interfaces.IInstructorLookup / Learning.Application.Interfaces.IUserContactLookup.
/// </summary>
public interface ICreatorProfileLookup
{
    Task<CreatorProfile?> GetProfileAsync(Guid creatorId, CancellationToken ct = default);
}
