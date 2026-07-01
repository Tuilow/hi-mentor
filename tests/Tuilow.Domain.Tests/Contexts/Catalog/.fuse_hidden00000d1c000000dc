using DogMaster.Domain.Contexts.Catalog.Entities;
using DogMaster.Domain.Contexts.Catalog.Enums;
using FluentAssertions;
using System;
using Xunit;

namespace DogMaster.Domain.Tests.Contexts.Catalog;

public sealed class CourseTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateDraftCourse()
    {
        var course = Course.Create(
            instructorId: Guid.NewGuid(),
            title: "Fundamentos do Adestramento",
            description: "Aprenda técnicas de adestramento do zero",
            level: CourseLevel.Beginner);

        course.Title.Should().Be("Fundamentos do Adestramento");
        course.Status.Should().Be(CourseStatus.Draft);
        course.Slug.Should().NotBeNull();
    }

    [Fact]
    public void Publish_WithoutModules_ShouldThrow()
    {
        var course = Course.Create(
            instructorId: Guid.NewGuid(),
            title: "Curso vazio",
            description: "Descrição do curso",
            level: CourseLevel.Beginner);

        var act = () => course.Publish();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*módulo*");
    }
}
