using MediatR;

namespace HiMentor.Catalog.Application.Queries.ListCoursesAdmin;

/// <summary>
/// Painel "Gerenciar Cursos" do próprio Creator autenticado — por isso exige InstructorId e
/// filtra por ele (ver ListCoursesAdminQueryHandler). Nunca deve listar cursos de outro criador.
/// </summary>
public sealed record ListCoursesAdminQuery(Guid InstructorId) : IRequest<IEnumerable<CourseAdminResponse>>;

public sealed record CourseAdminResponse(
    Guid Id,
    string Title,
    string Slug,
    string Level,
    string Status,
    decimal Price,
    bool IsFree,
    int ModuleCount,
    int LessonCount,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    string? Category,
    string ProductType,
    int ViewCount
);
