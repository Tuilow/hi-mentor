using MediatR;
using Tuilow.Catalog.Application.Queries.GetCourseBySlug;

namespace Tuilow.Catalog.Application.Queries.GetCourseByIdAdmin;

/// <summary>Busca curso por ID — sem filtro de status (inclui Draft e Archived).</summary>
public sealed record GetCourseByIdAdminQuery(Guid CourseId) : IRequest<CourseDetailResponse>;
