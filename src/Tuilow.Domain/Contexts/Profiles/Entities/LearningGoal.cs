using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Profiles.Entities;

public sealed class LearningGoal : Entity
{
    public Guid ProfileId { get; private set; }
    public string GoalType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsAchieved { get; private set; }
    public DateTime? AchievedAt { get; private set; }

    private LearningGoal() { }

    public static LearningGoal Create(Guid profileId, string goalType, string? description) =>
        new() { ProfileId = profileId, GoalType = goalType, Description = description };

    public void MarkAchieved()
    {
        IsAchieved = true;
        AchievedAt = DateTime.UtcNow;
        Touch();
    }
}
