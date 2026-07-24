using MediatR;
using Tuilow.Catalog.Application.Queries.GetCourseBySlug;

namespace Tuilow.Catalog.Application.Queries.GetCourseByIdAdmin;

/// <summary>
/// Busca curso por ID — sem filtro de status (inclui Draft e Archived). InstructorId é conferido
/// no handler (mesmo padrão de AddModuleCommandHandler etc.) — só o dono do curso pode abri-lo
/// no painel "Gerenciar Cursos".
/// </summary>
public sealed record GetCourseByIdAdminQuery(Guid CourseId, Guid InstructorId) : IRequest<CourseDetailResponse>;
