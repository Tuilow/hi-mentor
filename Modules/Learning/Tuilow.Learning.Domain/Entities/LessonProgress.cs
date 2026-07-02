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

    public void UpdateProgress(int watchedSeconds, int totalSeconds)
    {
        WatchedSeconds = watchedSeconds;
        LastWatchedAt = DateTime.UtcNow;
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
