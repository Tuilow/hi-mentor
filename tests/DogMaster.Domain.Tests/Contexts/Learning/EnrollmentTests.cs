using DogMaster.Domain.Contexts.Learning.Entities;
using DogMaster.Domain.Contexts.Learning.Enums;
using FluentAssertions;

namespace DogMaster.Domain.Tests.Contexts.Learning;

public sealed class EnrollmentTests
{
    [Fact]
    public void Create_ShouldHaveActiveStatus()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 10);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.ProgressPercent.Should().Be(0);
    }

    [Fact]
    public void TrackLessonProgress_At100Percent_ShouldCompleteLesson()
    {
        var lessonId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 1);
        enrollment.TrackLessonProgress(lessonId, 100, 100);

        var progress = enrollment.LessonProgress.First(p => p.LessonId == lessonId);
        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void TrackLessonProgress_AllLessonsComplete_ShouldMarkEnrollmentAsCompleted()
    {
        var lessonId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 1);
        enrollment.TrackLessonProgress(lessonId, 100, 100);

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CourseCompletedDomainEvent");
    }
}
