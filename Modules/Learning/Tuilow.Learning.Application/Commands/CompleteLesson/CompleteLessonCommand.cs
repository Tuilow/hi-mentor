using MediatR;

namespace Tuilow.Learning.Application.Commands.CompleteLesson;

public sealed record CompleteLessonCommand(
    Guid UserId,
    Guid EnrollmentId,
    Guid LessonId,
    int WatchedSeconds,
    int TotalSeconds
) : IRequest;
