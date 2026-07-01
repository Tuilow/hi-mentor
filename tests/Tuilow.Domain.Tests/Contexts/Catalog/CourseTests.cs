using Tuilow.Domain.Contexts.Catalog.Entities;
using Tuilow.Domain.Contexts.Catalog.Enums;
using FluentAssertions;
using System;
using Xunit;

namespace Tuilow.Domain.Tests.Contexts.Catalog;

public sealed class CourseTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateDraftCourse()
    {
        var course = Course.Create(
            instructorId: Guid.NewGuid(),
            title: "Fundamentos de Programação",
            description: "Aprenda técnicas de programação do zero",
            level: CourseLevel.Beginner);

        course.Title.Should().Be("Fundamentos de Programação");
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
