using MediatR;

namespace HiMentor.Catalog.Application.Commands.ArchiveCourse;

public sealed record ArchiveCourseCommand(Guid CourseId, Guid InstructorId) : IRequest;
