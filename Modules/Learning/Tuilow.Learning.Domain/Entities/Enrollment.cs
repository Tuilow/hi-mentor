using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Learning.Domain.Enums;
using Tuilow.Learning.Domain.Events;

namespace Tuilow.Learning.Domain.Entities;

public sealed class Enrollment : AggregateRoot
{
    private readonly List<LessonProgress> _lessonProgress = [];

    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public EnrollmentStatus Status { get; private set; } = EnrollmentStatus.Active;
    public decimal ProgressPercentage { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // Achado M12 da auditoria: sem isso, um chamado de suporte do tipo "paguei e não recebi
    // acesso" exigia cruzar manualmente Sales (CoursePurchase/Subscription) e Learning (Enrollment)
    // sem nenhum identificador em comum. Mutuamente exclusivos (uma matrícula vem de compra avulsa
    // OU de assinatura, nunca as duas) — null nos dois quando criada manualmente (EnrollStudentCommand).
    public Guid? SourcePurchaseId { get; private set; }
    public Guid? SourceSubscriptionId { get; private set; }

    public IReadOnlyCollection<LessonProgress> LessonsProgress => _lessonProgress.AsReadOnly();

    private Enrollment() { }

    public static Enrollment Create(
        Guid userId, Guid courseId, string courseTitle,
        Guid? sourcePurchaseId = null, Guid? sourceSubscriptionId = null)
    {
        var enrollment = new Enrollment
        {
            UserId = userId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow,
            SourcePurchaseId = sourcePurchaseId,
            SourceSubscriptionId = sourceSubscriptionId
        };

        enrollment.AddDomainEvent(new StudentEnrolledDomainEvent(enrollment.Id, userId, courseId, courseTitle));
        return enrollment;
    }

    /// <summary>
    /// Registra progresso na aula. Retorna o LessonProgress caso um NOVO registro tenha sido
    /// criado (para o caller persistir explicitamente como Added — evita DbUpdateConcurrencyException
    /// quando o Enrollment pai já está tracked). Retorna null se apenas atualizou um existente.
    /// </summary>
    public LessonProgress? TrackLessonProgress(Guid lessonId, int watchedSeconds, int totalSeconds, int totalLessons)
    {
        var progress = _lessonProgress.SingleOrDefault(p => p.LessonId == lessonId);
        LessonProgress? newProgress = null;
        if (progress is null)
        {
            progress = LessonProgress.Create(Id, lessonId);
            _lessonProgress.Add(progress);
            newProgress = progress;
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
        return newProgress;
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
