using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.AddLesson;

public sealed record AddLessonCommand(
    Guid CourseId,
    Guid ModuleId,
    string Title,
    string? Description,
    bool IsPreview = false
) : IRequest<Guid>;
