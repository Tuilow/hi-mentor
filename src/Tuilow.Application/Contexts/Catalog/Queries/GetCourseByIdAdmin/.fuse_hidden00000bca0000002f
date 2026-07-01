using MediatR;
using DogMaster.Application.Contexts.Catalog.Queries.GetCourseBySlug;

namespace DogMaster.Application.Contexts.Catalog.Queries.GetCourseByIdAdmin;

/// <summary>Busca curso por ID — sem filtro de status (inclui Draft e Archived).</summary>
public sealed record GetCourseByIdAdminQuery(Guid CourseId) : IRequest<CourseDetailResponse>;
