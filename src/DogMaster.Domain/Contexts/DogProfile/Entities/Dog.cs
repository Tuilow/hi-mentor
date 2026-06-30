using DogMaster.Domain.Common.Abstractions;
using DogMaster.Domain.Contexts.DogProfile.Enums;
using DogMaster.Domain.Contexts.DogProfile.Events;

namespace DogMaster.Domain.Contexts.DogProfile.Entities;

public sealed class Dog : AggregateRoot
{
    private readonly List<DogObjective> _objectives = [];

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Breed { get; private set; }
    public string? Sex { get; private set; }      // Male, Female
    public DateOnly? BirthDate { get; private set; }
    public decimal? WeightKg { get; private set; }
    public string? PhotoUrl { get; private set; }
    public DogLevel Level { get; private set; } = DogLevel.Puppy;
    public bool? IsNeutered { get; private set; }
    public string? Notes { get; private set; }

    public int? AgeMonths => BirthDate.HasValue
        ? (int)((DateTime.UtcNow - BirthDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44)
        : null;

    public IReadOnlyCollection<DogObjective> Objectives => _objectives.AsReadOnly();

    private Dog() { }

    public static Dog Create(Guid userId, string name, string? breed = null,
        string? sex = null, DateOnly? birthDate = null, decimal? weightKg = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var dog = new Dog
        {
            UserId = userId,
            Name = name.Trim(),
            Breed = breed?.Trim(),
            Sex = sex,
            BirthDate = birthDate,
            WeightKg = weightKg
        };

        dog.AddDomainEvent(new DogRegisteredDomainEvent(dog.Id, userId, dog.Name, breed));
        return dog;
    }

    public void Update(string name, string? breed, string? sex, DateOnly? birthDate,
        decimal? weightKg, bool? isNeutered, string? notes)
    {
        Name = name.Trim();
        Breed = breed?.Trim();
        Sex = sex;
        BirthDate = birthDate;
        WeightKg = weightKg;
        IsNeutered = isNeutered;
        Notes = notes;
        Touch();
    }

    public void SetPhoto(string photoUrl) { PhotoUrl = photoUrl; Touch(); }

    public void SetLevel(DogLevel level) { Level = level; Touch(); }

    public DogObjective AddObjective(string objectiveType, string? description)
    {
        var obj = DogObjective.Create(Id, objectiveType, description);
        _objectives.Add(obj);
        Touch();
        return obj;
    }
}
