using DogMaster.Domain.Common.Abstractions;
using DogMaster.Domain.Contexts.Catalog.Enums;
using DogMaster.Domain.Contexts.Catalog.Events;
using DogMaster.Domain.Contexts.Catalog.ValueObjects;

namespace DogMaster.Domain.Contexts.Catalog.Entities;

public sealed class Course : AggregateRoot
{
    private readonly List<Module> _modules = [];

    public Guid InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public Money Price { get; private set; } = Money.Free;
    public CourseLevel Level { get; private set; } = CourseLevel.Beginner;
    public CourseStatus Status { get; private set; } = CourseStatus.Draft;
    public bool IsFree => Price.IsZero;
    public DateTime? PublishedAt { get; private set; }

    public int TotalDurationMinutes =>
        _modules.SelectMany(m => m.Lessons)
                .Sum(l => (l.DurationSeconds ?? 0) / 60);

    public IReadOnlyCollection<Module> Modules => _modules.AsReadOnly();

    private Course() { }

    public static Course Create(
        Guid instructorId,
        string title,
        string description,
        CourseLevel level,
        decimal price = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Course
        {
            InstructorId = instructorId,
            Title = title.Trim(),
            Slug = Slug.Create(title),
            Description = description.Trim(),
            Level = level,
            Price = Money.Of(price)
        };
    }

    public void Update(string title, string description, string? shortDescription,
        CourseLevel level, decimal price, string? thumbnailUrl)
    {
        Title = title.Trim();
        Slug = Slug.Create(title);
        Description = description.Trim();
        ShortDescription = shortDescription?.Trim();
        Level = level;
        Price = Money.Of(price);
        ThumbnailUrl = thumbnailUrl;
        Touch();
    }

    public void Publish()
    {
        if (Status == CourseStatus.Published) return;
        if (!_modules.Any()) throw new InvalidOperationException("O curso precisa ter ao menos um módulo para ser publicado.");
        if (_modules.All(m => !m.Lessons.Any())) throw new InvalidOperationException("O curso precisa ter ao menos uma aula.");

        Status = CourseStatus.Published;
        PublishedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePublishedDomainEvent(Id, InstructorId, Title, Slug.Value));
    }

    public void Archive()
    {
        Status = CourseStatus.Archived;
        Touch();
    }

    public Module AddModule(string title, string? description)
    {
        var order = _modules.Count + 1;
        var module = Module.Create(Id, title, description, order);
        _modules.Add(module);
        Touch();
        return module;
    }
}
