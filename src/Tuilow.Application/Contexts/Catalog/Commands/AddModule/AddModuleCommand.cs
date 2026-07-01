using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.AddModule;

public sealed record AddModuleCommand(Guid CourseId, string Title, string? Description) : IRequest<Guid>;
