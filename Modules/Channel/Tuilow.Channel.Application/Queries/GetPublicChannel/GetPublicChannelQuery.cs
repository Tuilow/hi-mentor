using MediatR;

namespace Tuilow.Channel.Application.Queries.GetPublicChannel;

/// <summary>
/// Perfil público do Canal do Criador (tuilow.com/canal/{handle}) — vitrine com todos os cursos
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
    IReadOnlyList<PublicChannelCourse> Courses
);

public sealed record PublicSocialLink(string Platform, string Url);

public sealed record PublicChannelCourse(
    Guid Id,
    string Title,
    string Slug,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    bool IsUnlocked
);
