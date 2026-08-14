using MediatR;

namespace HiMentor.Learning.Application.Commands.CompleteLesson;

/// <summary>
/// ClientCapturedAt (achado M6 da avaliação): instante em que o CLIENTE leu o currentTime do
/// vídeo, não quando a requisição chegou aqui — deixa o domínio distinguir um evento antigo
/// chegando atrasado (ex.: celular com rede lenta) de um evento realmente mais novo, em vez de
/// só aplicar "quem chegou por último". Opcional para não quebrar clientes desatualizados.
/// </summary>
public sealed record CompleteLessonCommand(
    Guid UserId,
    Guid EnrollmentId,
    Guid LessonId,
    int WatchedSeconds,
    int TotalSeconds,
    DateTime? ClientCapturedAt = null
) : IRequest;
