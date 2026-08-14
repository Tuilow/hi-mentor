using MediatR;

namespace HiMentor.Channel.Application.Commands.UpsertChannel;

/// <summary>
/// Cria o canal do criador na primeira vez (define o @handle) ou atualiza handle/redes sociais
/// depois. "Upsert" porque, do ponto de vista do criador, é sempre "meu canal" — um único
/// formulário, sem tela separada de criar-vs-editar.
/// </summary>
public sealed record UpsertChannelCommand(
    Guid CreatorId,
    string Handle,
    IReadOnlyList<SocialLinkInput> SocialLinks,
    string? BannerUrl = null,
    string? IntroVideoUrl = null
) : IRequest<Guid>;

public sealed record SocialLinkInput(string Platform, string Url);
