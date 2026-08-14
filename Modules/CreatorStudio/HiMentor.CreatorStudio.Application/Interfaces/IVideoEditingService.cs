namespace HiMentor.CreatorStudio.Application.Interfaces;

/// <summary>Se a edição automática de vídeo está disponível nesta instalação — usado pelo front pra mostrar a seção como "em breve" sem quebrar.</summary>
public sealed record VideoEditingCapabilities(bool IsAvailable, string StatusMessage);

/// <summary>Resultado da edição automática (silêncio removido, ruído reduzido, legendas, capítulos, thumbnail).</summary>
public sealed record VideoAutoEditResult(
    bool Completed,
    string? OutputVideoUrl,
    string? ThumbnailUrl,
    IReadOnlyList<string> ChapterTitles
);

/// <summary>Clipe sugerido para redes sociais a partir de um trecho do vídeo original.</summary>
public sealed record SocialClipSuggestion(
    string Format,
    string Title,
    TimeSpan Start,
    TimeSpan End,
    string? CaptionText
);

/// <summary>
/// Edição automática de vídeo com IA (item 8 do Estúdio do Criador: remoção de silêncio,
/// redução de ruído, legendas automáticas, capítulos, thumbnail) e geração de clipes para
/// redes sociais (item 9: Reels/Shorts/TikTok). Porta preparada, sem processamento real ainda —
/// não há ffmpeg nem serviço de IA de vídeo configurado nesta instalação (decisão explícita:
/// deixar pronta para plugar um provedor real depois, mesmo padrão do IWhatsAppService).
/// </summary>
public interface IVideoEditingService
{
    /// <summary>O front chama isso para saber se deve mostrar os botões de edição automática/clipes ou o aviso de "em breve".</summary>
    Task<VideoEditingCapabilities> GetCapabilitiesAsync(CancellationToken ct = default);

    Task<VideoAutoEditResult> AutoEditAsync(Guid videoId, CancellationToken ct = default);

    Task<IReadOnlyList<SocialClipSuggestion>> GenerateSocialClipsAsync(Guid videoId, CancellationToken ct = default);
}
