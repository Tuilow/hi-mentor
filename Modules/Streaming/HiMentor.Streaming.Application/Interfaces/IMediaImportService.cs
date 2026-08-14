using HiMentor.Streaming.Domain.Enums;

namespace HiMentor.Streaming.Application.Interfaces;

/// <summary>Metadados obtidos (best-effort) ao importar um vídeo por URL externa.</summary>
public sealed record ImportedMediaMetadata(
    VideoSource Source,
    string ExternalUrl,
    string? ExternalId,
    string? Title,
    string? ThumbnailUrl,
    int? DurationSeconds
);

/// <summary>
/// Passo 2 do assistente ("Conteúdo") — importar vídeo de uma plataforma externa em vez de
/// subir o arquivo, reduzindo custo de armazenamento (estratégia explícita do wizard: sempre
/// preferir import a upload local quando possível).
///
/// YouTube/Vimeo usam os respectivos endpoints públicos de oEmbed (sem necessidade de API key)
/// para buscar título/thumbnail/duração de verdade. Google Drive/Dropbox/OneDrive/Cloudflare
/// Stream não têm oEmbed público equivalente sem OAuth — nesses casos o serviço apenas
/// reconhece a plataforma pela URL e guarda a referência (best-effort); título/duração podem
/// ser completados manualmente pelo criador.
/// </summary>
public interface IMediaImportService
{
    Task<ImportedMediaMetadata> FetchMetadataAsync(string url, CancellationToken ct = default);
}
