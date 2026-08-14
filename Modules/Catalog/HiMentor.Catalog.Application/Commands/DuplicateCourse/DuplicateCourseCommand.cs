using MediatR;

namespace HiMentor.Catalog.Application.Commands.DuplicateCourse;

/// <summary>
/// Duplica um produto (estrutura de módulos/aulas, vídeos vinculados, materiais, preço,
/// categoria) como um novo rascunho — útil para reaproveitar um curso como base de outro
/// sem reconstruir tudo no assistente. A cópia nasce sempre em Draft, nunca publicada.
/// </summary>
public sealed record DuplicateCourseCommand(Guid CourseId, Guid InstructorId) : IRequest<Guid>;
