using MediatR;

namespace HiMentor.Catalog.Application.Commands.ReorderModules;

/// <summary>Achado B6 da avaliação: reordenar módulos por arrastar-e-soltar — o backend agora
/// suporta; a UI de drag-and-drop no assistente fica fora do escopo deste achado pontual.</summary>
public sealed record ReorderModulesCommand(Guid CourseId, Guid InstructorId, IReadOnlyList<Guid> OrderedModuleIds) : IRequest;
