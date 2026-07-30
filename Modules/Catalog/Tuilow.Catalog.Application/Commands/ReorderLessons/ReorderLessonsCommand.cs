using MediatR;

namespace Tuilow.Catalog.Application.Commands.ReorderLessons;

/// <summary>Achado B6 da avaliação: reordenar aulas dentro de um módulo por arrastar-e-soltar —
/// o backend agora suporta; a UI de drag-and-drop no assistente fica fora do escopo deste
/// achado pontual.</summary>
public sealed record ReorderLessonsCommand(
    Guid CourseId, Guid ModuleId, Guid InstructorId, IReadOnlyList<Guid> OrderedLessonIds) : IRequest;
