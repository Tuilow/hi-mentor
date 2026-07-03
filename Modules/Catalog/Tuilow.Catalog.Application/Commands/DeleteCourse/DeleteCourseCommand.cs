using MediatR;

namespace Tuilow.Catalog.Application.Commands.DeleteCourse;

public sealed record DeleteCourseCommand(Guid CourseId, Guid InstructorId) : IRequest;
