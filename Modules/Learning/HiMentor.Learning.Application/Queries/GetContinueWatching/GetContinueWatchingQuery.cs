using MediatR;

namespace HiMentor.Learning.Application.Queries.GetContinueWatching;

/// <summary>
/// "Continuar de onde parei" — a última aula assistida entre TODOS os cursos em que o aluno
/// está matriculado (não só um curso específico). Alimenta o atalho de destaque no dashboard.
/// Retorna null quando o aluno nunca assistiu nenhuma aula ainda.
/// </summary>
public sealed record GetContinueWatchingQuery(Guid UserId) : IRequest<ContinueWatchingResponse?>;

public sealed record ContinueWatchingResponse(
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    string? ThumbnailUrl,
    Guid LessonId,
    string LessonTitle,
    decimal CourseProgressPercentage,
    DateTime LastWatchedAt
);
