using MediatR;

namespace HiMentor.Channel.Application.Queries.GetPublicChannel;

/// <summary>
/// Perfil público do Canal do Criador (himentor.com/canal/{handle}) — vitrine com todos os cursos
/// publicados do criador. ViewerUserId é opcional (visitante anônimo) e, quando presente, é
/// usado só para marcar quais cursos o visitante já tem acesso (IsUnlocked) — nunca para
/// decidir se o canal pode ser visto (o canal em si é sempre público).
/// </summary>
public sealed record GetPublicChannelQuery(string Handle, Guid? ViewerUserId) : IRequest<PublicChannelResponse?>;

public sealed record PublicChannelResponse(
    Guid ChannelId,
    string Handle,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyList<PublicSocialLink> SocialLinks,
    IReadOnlyList<PublicChannelCourse> Courses,
    string? BannerUrl,
    string? IntroVideoUrl
);

public sealed record PublicSocialLink(string Platform, string Url);

public sealed record PublicChannelCourse(
    Guid Id,
    string Title,
    string Slug,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    bool IsUnlocked,
    // Estado real de comercialização ("Free"/"Paid"/"Subscription"/"Hidden") — ver
    // CourseCommercializationResolver. Price/IsFree seguem existindo (compatibilidade), mas o
    // front-end deve exibir "Grátis"/preço a partir deste campo, nunca derivar de novo.
    string CommercializationState
);
