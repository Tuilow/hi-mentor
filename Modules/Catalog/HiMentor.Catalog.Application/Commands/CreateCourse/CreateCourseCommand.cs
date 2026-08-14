using HiMentor.Catalog.Domain.Enums;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.CreateCourse;

public sealed record CreateCourseCommand(
    Guid InstructorId,
    string Title,
    string Description,
    string? ShortDescription,
    CourseLevel Level,
    decimal Price = 0,
    ProductType ProductType = ProductType.Course
) : IRequest<Guid>;
