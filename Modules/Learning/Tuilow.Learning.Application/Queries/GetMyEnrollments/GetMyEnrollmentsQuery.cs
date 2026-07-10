using MediatR;

namespace Tuilow.Learning.Application.Queries.GetMyEnrollments;

/// <summary>
/// "Meus cursos matriculados" — usado pelo filtro "Matriculados" na listagem de cursos do
/// aluno. Cruza Enrollment (Learning) com Course (Catalog); ver handler para o porquê desse
/// acoplamento ser aceitável aqui (mesmo padrão de EnrollStudentCommandHandler).
/// </summary>
public sealed record GetMyEnrollmentsQuery(Guid UserId) : IRequest<IEnumerable<MyEnrollmentResponse>>;

public sealed record MyEnrollmentResponse(
    Guid EnrollmentId,
    Guid CourseId,
    string Title,
    string Slug,
    string? ThumbnailUrl,
    decimal Price,
    bool IsFree,
    string Level,
    string Status,
    decimal ProgressPercentage,
    DateTime EnrolledAt,
    DateTime? CompletedAt,
    int CompletedLessonsCount
);
