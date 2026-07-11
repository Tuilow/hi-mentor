using MediatR;

namespace Tuilow.Catalog.Application.Commands.PublishCourse;

public sealed record PublishCourseCommand(Guid CourseId, Guid InstructorId) : IRequest;
