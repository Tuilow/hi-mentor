using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.ListCoursesAdmin;

public sealed class ListCoursesAdminQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<ListCoursesAdminQuery, IEnumerable<CourseAdminResponse>>
{
    public async Task<IEnumerable<CourseAdminResponse>> Handle(
        ListCoursesAdminQuery request, CancellationToken ct)
    {
        // Mesmo método já usado pela tela "Meus Produtos" (ver comentário em
        // ICourseRepository.ListByInstructorAsync) — antes este handler chamava
        // ListAllForAdminAsync (sem filtro nenhum), o que vazava cursos de outros criadores
        // para qualquer Creator autenticado no painel "Gerenciar Cursos".
        var courses = await courseRepository.ListByInstructorAsync(request.InstructorId, ct);

        return courses.Select(c => new CourseAdminResponse(
            c.Id,
            c.Title,
            c.Slug.Value,
            c.Level.ToString(),
            c.Status.ToString(),
            c.Price.Amount,
            c.IsFree,
            c.Modules.Count,
            c.Modules.SelectMany(m => m.Lessons).Count(),
            c.CreatedAt,
            c.PublishedAt,
            c.Category,
            c.ProductType.ToString(),
            c.ViewCount
        ));
    }
}
