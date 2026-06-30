using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Catalog.Entities;

public sealed class Lesson : Entity
{
    private readonly List<LessonAttachment> _attachments = [];
    private readonly List<LessonExercise> _exercises = [];

    public Guid ModuleId { get; private set; }
    public Guid? VideoId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public int? DurationSeconds { get; private set; }
    public bool IsPreview { get; private set; }

    public IReadOnlyCollection<LessonAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyCollection<LessonExercise> Exercises => _exercises.AsReadOnly();

    private Lesson() { }

    public static Lesson Create(Guid moduleId, string title, string? description, int order, bool isPreview = false) =>
        new()
        {
            ModuleId = moduleId,
            Title = title.Trim(),
            Description = description,
            Order = order,
            IsPreview = isPreview
        };

    public void SetVideo(Guid videoId, int durationSeconds)
    {
        VideoId = videoId;
        DurationSeconds = durationSeconds;
        Touch();
    }

    public LessonAttachment AddAttachment(string title, string fileUrl, string? fileType = null, long? size = null)
    {
        var attachment = LessonAttachment.Create(Id, title, fileUrl, fileType, size);
        _attachments.Add(attachment);
        Touch();
        return attachment;
    }

    public LessonExercise AddExercise(string title, string? description)
    {
        var order = _exercises.Count + 1;
        var exercise = LessonExercise.Create(Id, title, description, order);
        _exercises.Add(exercise);
        Touch();
        return exercise;
    }
}
