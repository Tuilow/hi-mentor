using MediatR;

namespace Tuilow.Streaming.Application.Queries.GetVideosByCourse;

/// <summary>
/// Reidrata o passo 2 do assistente ("Conteúdo") com os vídeos já enviados/importados para
/// este produto — necessário porque a lista de vídeos antes só existia na memória da página e
/// "sumia" se o criador saísse e voltasse ao assistente antes de vincular o vídeo a uma aula.
/// </summary>
public sealed record GetVideosByCourseQuery(Guid CourseId, Guid InstructorId) : IRequest<IEnumerable<VideoSummaryResponse>>;

public sealed record VideoSummaryResponse(
    Guid VideoId,
    string? Title,
    string Source,
    int? DurationSeconds,
    string? ThumbnailUrl,
    bool IsLinkedToLesson
);
