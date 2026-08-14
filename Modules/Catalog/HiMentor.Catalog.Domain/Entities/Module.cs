using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Catalog.Domain.Entities;

public sealed class Module : Entity
{
    private readonly List<Lesson> _lessons = [];

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }

    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();

    private Module() { }

    public static Module Create(Guid courseId, string title, string? description, int order) =>
        new() { CourseId = courseId, Title = title.Trim(), Description = description, Order = order };

    public Lesson AddLesson(string title, string? description, bool isPreview = false)
    {
        var order = _lessons.Count + 1;
        var lesson = Lesson.Create(Id, title, description, order, isPreview);
        _lessons.Add(lesson);
        Touch();
        return lesson;
    }

    public void Reorder(int newOrder)
    {
        Order = newOrder;
        Touch();
    }
}
