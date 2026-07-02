using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Journey.Domain.Enums;
using Tuilow.Journey.Domain.Events;

namespace Tuilow.Journey.Domain.Entities;

/// <summary>
/// Perfil de aprendizado de um usuário. Um usuário pode manter múltiplos perfis
/// (ex.: familiares, turmas ou áreas de interesse distintas), cada um com seu
/// próprio nível de proficiência, metas de aprendizado e progresso.
/// </summary>
public sealed class LearnerProfile : AggregateRoot
{
    private readonly List<LearningGoal> _goals = [];

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Category { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? PhotoUrl { get; private set; }
    public ProficiencyLevel Level { get; private set; } = ProficiencyLevel.Beginner;
    public string? Notes { get; private set; }

    public int? AgeMonths => BirthDate.HasValue
        ? (int)((DateTime.UtcNow - BirthDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 30.44)
        : null;

    public IReadOnlyCollection<LearningGoal> Goals => _goals.AsReadOnly();

    private LearnerProfile() { }

    public static LearnerProfile Create(Guid userId, string name, string? category = null,
        DateOnly? birthDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var profile = new LearnerProfile
        {
            UserId = userId,
            Name = name.Trim(),
            Category = category?.Trim(),
            BirthDate = birthDate
        };

        profile.AddDomainEvent(new LearnerProfileRegisteredDomainEvent(profile.Id, userId, profile.Name, category));
        return profile;
    }

    public void Update(string name, string? category, DateOnly? birthDate, string? notes)
    {
        Name = name.Trim();
        Category = category?.Trim();
        BirthDate = birthDate;
        Notes = notes;
        Touch();
    }

    public void SetPhoto(string photoUrl) { PhotoUrl = photoUrl; Touch(); }

    public void SetLevel(ProficiencyLevel level) { Level = level; Touch(); }

    public LearningGoal AddGoal(string goalType, string? description)
    {
        var goal = LearningGoal.Create(Id, goalType, description);
        _goals.Add(goal);
        Touch();
        return goal;
    }
}
