using MediatR;

namespace HiMentor.Streaming.Application.Commands.ImportExternalVideo;

/// <summary>
/// Download=true baixa o vídeo de verdade e hospeda no Cloudflare Stream (checkbox "baixar
/// vídeo" do passo 2 do assistente) em vez de só guardar o link — hoje só suportado para
/// YouTube (ver ImportExternalVideoCommandHandler).
/// </summary>
public sealed record ImportExternalVideoCommand(Guid CourseId, Guid InstructorId, string Url, bool Download = false)
    : IRequest<ImportExternalVideoResponse>;

public sealed record ImportExternalVideoResponse(
    Guid VideoId,
    string Source,
    string? Title,
    string? ThumbnailUrl,
    int? DurationSeconds,
    string Status
);
