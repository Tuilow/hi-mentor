using DogMaster.Domain.Contexts.Catalog.Entities;
using DogMaster.Domain.Contexts.Catalog.Enums;
using FluentAssertions;

namespace DogMaster.Domain.Tests.Contexts.Catalog;

public sealed class CourseTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateDraftCourse()
    {
        var course = Course.Create(
            "Fundamentos do Adestramento",
            "curso-fundamentos",
            "Aprenda desde o início",
            "Conteúdo completo",
            CourseLevel.Beginner,
            Guid.NewGuid());

        course.Title.Should().Be("Fundamentos do Adestramento");
        course.Status.Should().Be(CourseStatus.Draft);
        course.Slug.Should().NotBeNull();
    }

    [Fact]
    public void Publish_WithoutModules_ShouldThrow()
    {
        var course = Course.Create(
            "Curso vazio", "curso-vazio", "Desc", "Full",
            CourseLevel.Beginner, Guid.NewGuid());

        var act = () => course.Publish();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*módulo*");
    }
}
