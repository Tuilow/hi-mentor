using MediatR;

namespace HiMentor.Channel.Application.Queries.GetMyChannel;

/// <summary>Tela de configuração do canal — retorna null se o criador ainda não criou um.</summary>
public sealed record GetMyChannelQuery(Guid CreatorId) : IRequest<MyChannelResponse?>;

public sealed record MyChannelResponse(
    Guid Id,
    string Handle,
    IReadOnlyList<SocialLinkResponse> SocialLinks,
    string? BannerUrl,
    string? IntroVideoUrl
);

public sealed record SocialLinkResponse(string Platform, string Url);
