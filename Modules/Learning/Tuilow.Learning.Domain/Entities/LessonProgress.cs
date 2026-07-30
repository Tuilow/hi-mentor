using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Entities;

public sealed class LessonProgress : Entity
{
    public Guid EnrollmentId { get; private set; }
    public Guid LessonId { get; private set; }
    public int WatchedSeconds { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime LastWatchedAt { get; private set; }

    private LessonProgress() { }

    public static LessonProgress Create(Guid enrollmentId, Guid lessonId) =>
        new() { EnrollmentId = enrollmentId, LessonId = lessonId, LastWatchedAt = DateTime.UtcNow };

    /// <summary>
    /// Achado M6 da avaliação: sem clientCapturedAt, "quem salva por último vence" mesmo que o
    /// evento seja mais antigo — ex.: celular grava aos 8min e, atrasado pela rede, chega DEPOIS
    /// do notebook já ter salvo 15min; o ponto de retomada regredia sem motivo. clientCapturedAt
    /// é o instante em que o CLIENTE leu o currentTime (não quando a requisição chegou aqui). Um
    /// evento mais antigo que o último aplicado E que reduziria o segundo assistido é descartado
    /// — mas só nesse caso: um evento antigo com valor MAIOR (ex.: o aluno pulou pra frente antes
    /// de outro dispositivo reportar um valor menor) ainda é aplicado normalmente, e a conclusão
    /// (IsCompleted) nunca regride de qualquer forma. clientCapturedAt ausente (clientes antigos)
    /// aplica sempre, sem quebrar compatibilidade.
    /// </summary>
    public void UpdateProgress(int watchedSeconds, int totalSeconds, DateTime? clientCapturedAt = null)
    {
        var isStaleRegression = clientCapturedAt is { } capturedAt
            && capturedAt < LastWatchedAt
            && watchedSeconds < WatchedSeconds;
        if (isStaleRegression) return;

        WatchedSeconds = watchedSeconds;
        LastWatchedAt = clientCapturedAt ?? DateTime.UtcNow;
        Touch();

        // Considera concluída se assistiu >= 90%
        if (!IsCompleted && totalSeconds > 0 && watchedSeconds >= totalSeconds * 0.9)
            Complete();
    }

    public void Complete()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
}
