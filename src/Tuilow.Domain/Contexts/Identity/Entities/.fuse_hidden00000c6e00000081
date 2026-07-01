using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Identity.Entities;

public sealed class UserProfile : Entity
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? Bio { get; private set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private UserProfile() { }

    public static UserProfile Create(Guid userId, string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        return new UserProfile
        {
            UserId = userId,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim()
        };
    }

    public void Update(string firstName, string lastName, string? phone, DateOnly? birthDate, string? bio)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Phone = phone?.Trim();
        BirthDate = birthDate;
        Bio = bio?.Trim();
        Touch();
    }

    public void SetAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
        Touch();
    }
}
