using MediatR;

namespace HiMentor.Learning.Application.Queries.GetLessonHistory;

/// <summary>
/// Histórico de aulas assistidas — todas as aulas com progresso registrado (assistidas ou em
/// andamento), entre todos os cursos em que o aluno está matriculado, mais recentes primeiro.
/// Diferente de GetContinueWatching (que devolve só a última aula única), aqui é a lista
/// completa para a tela "Meu histórico" da Área do Aluno.
/// </summary>
public sealed record GetLessonHistoryQuery(Guid UserId) : IRequest<IEnumerable<LessonHistoryItemResponse>>;

public sealed record LessonHistoryItemResponse(
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    string? ThumbnailUrl,
    Guid LessonId,
    string LessonTitle,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime LastWatchedAt
);
