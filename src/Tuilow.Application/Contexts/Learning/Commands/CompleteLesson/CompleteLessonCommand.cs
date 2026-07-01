using MediatR;

namespace Tuilow.Application.Contexts.Learning.Commands.CompleteLesson;

public sealed record CompleteLessonCommand(
    Guid UserId,
    Guid EnrollmentId,
    Guid LessonId,
    int WatchedSeconds,
    int TotalSeconds
) : IRequest;
