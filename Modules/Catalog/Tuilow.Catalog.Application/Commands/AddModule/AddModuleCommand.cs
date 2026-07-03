using MediatR;

namespace Tuilow.Catalog.Application.Commands.AddModule;

public sealed record AddModuleCommand(Guid CourseId, Guid InstructorId, string Title, string? Description) : IRequest<Guid>;
