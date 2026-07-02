using Tuilow.Catalog.Domain.Enums;
using MediatR;

namespace Tuilow.Catalog.Application.Commands.CreateCourse;

public sealed record CreateCourseCommand(
    Guid InstructorId,
    string Title,
    string Description,
    string? ShortDescription,
    CourseLevel Level,
    decimal Price = 0
) : IRequest<Guid>;
