using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Contexts.Learning.Enums;
using Tuilow.Domain.Contexts.Learning.Events;

namespace Tuilow.Domain.Contexts.Learning.Entities;

public sealed class Enrollment : AggregateRoot
{
    private readonly List<LessonProgress> _lessonProgress = [];

    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Active;
    public decimal ProgressPercentage { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public IReadOnlyCollection<LessonProgress> LessonsProgress => _lessonProgress.AsReadOnly();

    private Enrollment() { }

    public static Enrollment Create(Guid userId, Guid courseId, string courseTitle)
    {
        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow
        };

        enrollment.AddDomainEvent(new StudentEnrolledDomainEvent(enrollment.Id, userId, courseId, courseTitle));
        return enrollment;
    }

    public void TrackLessonProgress(Guid lessonId, int watchedSeconds, int totalSeconds, int totalLessons)
    {
        var progress = _lessonProgress.SingleOrDefault(p => p.LessonId == lessonId);
        if (progress is null)
        {
            progress = LessonProgress.Create(Id, lessonId);
            _lessonProgress.Add(progress);
        }

        var wasCompleted = progress.IsCompleted;
        progress.UpdateProgress(watchedSeconds, totalSeconds);

        if (!wasCompleted && progress.IsCompleted)
        {
            RecalculateProgress(totalLessons);
            AddDomainEvent(new LessonCompletedDomainEvent(Id, UserId, lessonId, ProgressPercentage));

            if (ProgressPercentage >= 100)
                Complete();
        }

        Touch();
    }

    private void RecalculateProgress(int totalLessons)
    {
        if (totalLessons == 0) return;
        var completed = _lessonProgress.Count(p => p.IsCompleted);
        ProgressPercentage = Math.Round((decimal)completed / totalLessons * 100, 2);
    }

    private void Complete()
    {
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new CourseCompletedDomainEvent(Id, UserId, CourseId));
    }

    public void Cancel()
    {
        Status = EnrollmentStatus.Cancelled;
        Touch();
    }
}
