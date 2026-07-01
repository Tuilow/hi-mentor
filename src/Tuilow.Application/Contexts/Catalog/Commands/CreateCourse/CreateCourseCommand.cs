using Tuilow.Domain.Contexts.Catalog.Enums;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.CreateCourse;

public sealed record CreateCourseCommand(
    Guid InstructorId,
    string Title,
    string Description,
    string? ShortDescription,
    CourseLevel Level,
    decimal Price = 0
) : IRequest<Guid>;
