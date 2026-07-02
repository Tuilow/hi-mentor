using MediatR;

namespace Tuilow.Catalog.Application.Commands.AddModule;

public sealed record AddModuleCommand(Guid CourseId, string Title, string? Description) : IRequest<Guid>;
