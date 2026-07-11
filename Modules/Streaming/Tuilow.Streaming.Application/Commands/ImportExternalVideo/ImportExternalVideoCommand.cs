using MediatR;

namespace Tuilow.Streaming.Application.Commands.ImportExternalVideo;

public sealed record ImportExternalVideoCommand(Guid CourseId, Guid InstructorId, string Url) : IRequest<ImportExternalVideoResponse>;

public sealed record ImportExternalVideoResponse(
    Guid VideoId,
    string Source,
    string? Title,
    string? ThumbnailUrl,
    int? DurationSeconds
);
