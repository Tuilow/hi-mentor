using DogMaster.Domain.Contexts.Learning.Entities;
using DogMaster.Domain.Contexts.Learning.Enums;
using FluentAssertions;
using Xunit;

namespace DogMaster.Domain.Tests.Contexts.Learning;

public sealed class EnrollmentTests
{
    [Fact]
    public void Create_ShouldHaveActiveStatus()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), "Curso de Adestramento");
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public void TrackLessonProgress_At100Percent_ShouldCompleteLesson()
    {
        var lessonId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), "Curso de Adestramento");
        enrollment.TrackLessonProgress(lessonId, 100, 100, totalLessons: 2);

        var progress = enrollment.LessonsProgress.First(p => p.LessonId == lessonId);
        progress.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void TrackLessonProgress_AllLessonsComplete_ShouldMarkEnrollmentAsCompleted()
    {
        var lessonId = Guid.NewGuid();
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), "Curso de Adestramento");
        enrollment.TrackLessonProgress(lessonId, 100, 100, totalLessons: 1);

        enrollment.Status.Should().Be(EnrollmentStatus.Completed);
        enrollment.CompletedAt.Should().NotBeNull();
        enrollment.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "CourseCompletedDomainEvent");
    }
}
