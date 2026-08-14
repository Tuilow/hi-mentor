using MediatR;

namespace HiMentor.Streaming.Application.Queries.GetLessonPlayUrl;

public sealed record GetLessonPlayUrlQuery(
    Guid CourseId,
    Guid LessonId,
    Guid? CurrentUserId
) : IRequest<LessonPlayUrlResponse>;

public sealed record LessonPlayUrlResponse(
    Guid LessonId,
    string Title,
    bool IsPreview,
    string PlaybackUrl,
    int? DurationSeconds,
    string? ThumbnailUrl
);
