using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.DogProfile.Entities;

public sealed class DogObjective : Entity
{
    public Guid DogId { get; private set; }
    public string ObjectiveType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAchieved { get; private set; }
    public DateTime? AchievedAt { get; private set; }

    private DogObjective() { }

    public static DogObjective Create(Guid dogId, string objectiveType, string? description) =>
        new() { DogId = dogId, ObjectiveType = objectiveType, Description = description };

    public void MarkAchieved()
    {
        IsAchieved = true;
        AchievedAt = DateTime.UtcNow;
        Touch();
    }
}
