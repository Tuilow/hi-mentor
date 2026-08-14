using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Catalog.Domain.Entities;

public sealed class LessonExercise : Entity
{
    public Guid LessonId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }

    private LessonExercise() { }

    public static LessonExercise Create(Guid lessonId, string title, string? description, int order) =>
        new() { LessonId = lessonId, Title = title.Trim(), Description = description, Order = order };
}
