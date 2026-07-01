using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.PublishCourse;

public sealed record PublishCourseCommand(Guid CourseId, Guid InstructorId) : IRequest;
