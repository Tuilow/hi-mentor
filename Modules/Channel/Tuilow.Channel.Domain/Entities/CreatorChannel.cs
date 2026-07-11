using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Channel.Domain.ValueObjects;

namespace Tuilow.Channel.Domain.Entities;

/// <summary>Link para uma rede social exibido no Canal do Criador.</summary>
public sealed record SocialLink(string Platform, string Url);

/// <summary>
/// Canal do Criador — perfil público (tuilow.com/canal/@handle) com a vitrine de todos os
/// cursos publicados do criador. Nome de exibição, avatar e bio NÃO são duplicados aqui: vêm de
/// IdentidadeAcesso.UserProfile (mesmo criador, mesma fonte de verdade — ver
/// Channel.Application.Interfaces.ICreatorProfileLookup). CreatorChannel existe só para o que é
/// exclusivo do conceito de canal: o @handle público e a lista de redes sociais. O catálogo de
/// cursos do canal também não é dado deste agregado — é composto em tempo de leitura a partir de
/// Catalog.ICourseRepository.ListByInstructorAsync (mesmo padrão de acoplamento legítimo já
/// usado por CreatorStudio).
/// </summary>
public sealed class CreatorChannel : AggregateRoot
{
    private readonly List<SocialLink> _socialLinks = [];

    public Guid CreatorId { get; private set; }
    public Handle Handle { get; private set; } = null!;
    public IReadOnlyCollection<SocialLink> SocialLinks => _socialLinks.AsReadOnly();

    /// <summary>Banner de topo da vitrine pública (URL de imagem) — opcional.</summary>
    public string? BannerUrl { get; private set; }

    /// <summary>Vídeo de apresentação do criador, exibido na vitrine pública (URL YouTube/Vimeo) — opcional.</summary>
    public string? IntroVideoUrl { get; private set; }

    private CreatorChannel() { }

    public static CreatorChannel Create(Guid creatorId, string handle)
    {
        return new CreatorChannel
        {
            CreatorId = creatorId,
            Handle = Handle.Create(handle)
        };
    }

    public void ChangeHandle(string handle)
    {
        Handle = Handle.Create(handle);
        Touch();
    }

    public void SetSocialLinks(IEnumerable<SocialLink> links)
    {
        _socialLinks.Clear();
        _socialLinks.AddRange(links.Where(l => !string.IsNullOrWhiteSpace(l.Url)));
        Touch();
    }

    /// <summary>Define banner e vídeo de apresentação da vitrine pública — ambos opcionais.</summary>
    public void SetBranding(string? bannerUrl, string? introVideoUrl)
    {
        BannerUrl = string.IsNullOrWhiteSpace(bannerUrl) ? null : bannerUrl.Trim();
        IntroVideoUrl = string.IsNullOrWhiteSpace(introVideoUrl) ? null : introVideoUrl.Trim();
        Touch();
    }
}
