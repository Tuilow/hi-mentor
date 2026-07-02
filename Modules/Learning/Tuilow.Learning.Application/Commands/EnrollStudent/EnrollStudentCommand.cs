using MediatR;

namespace Tuilow.Learning.Application.Commands.EnrollStudent;

public sealed record EnrollStudentCommand(Guid UserId, Guid CourseId) : IRequest<Guid>;
