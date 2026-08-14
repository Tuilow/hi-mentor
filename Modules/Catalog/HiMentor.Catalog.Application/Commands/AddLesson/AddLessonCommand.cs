using MediatR;

namespace HiMentor.Catalog.Application.Commands.AddLesson;

public sealed record AddLessonCommand(
    Guid CourseId,
    Guid InstructorId,
    Guid ModuleId,
    string Title,
    string? Description,
    bool IsPreview = false
) : IRequest<Guid>;
