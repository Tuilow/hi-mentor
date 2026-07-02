using MediatR;

namespace Tuilow.Catalog.Application.Commands.AddLesson;

public sealed record AddLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string? Description,
    bool IsPreview = false
) : IRequest<Guid>;
