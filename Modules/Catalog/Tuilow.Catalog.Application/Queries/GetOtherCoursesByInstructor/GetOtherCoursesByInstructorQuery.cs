using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetOtherCoursesByInstructor;

/// <summary>
/// Cross-sell automático: outros cursos publicados do mesmo criador. Usado na página do curso
/// do aluno ("Mais cursos deste professor"), na página de vendas pública e no Canal do Criador.
/// Público — não exige autenticação. Reaproveita ICourseRepository.ListByInstructorAsync (já
/// existente para a tela "Meus Produtos"), só filtrando por Published aqui.
/// </summary>
public sealed record GetOtherCoursesByInstructorQuery(Guid InstructorId, Guid? ExcludeCourseId)
    : IRequest<IEnumerable<InstructorCourseSummary>>;

public sealed record InstructorCourseSummary(
    Guid Id,
    string Title,
    string Slug,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level
);
