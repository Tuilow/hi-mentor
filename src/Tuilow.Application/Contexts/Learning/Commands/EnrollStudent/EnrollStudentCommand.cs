using MediatR;

namespace Tuilow.Application.Contexts.Learning.Commands.EnrollStudent;

public sealed record EnrollStudentCommand(Guid UserId, Guid CourseId) : IRequest<Guid>;
