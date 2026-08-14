namespace HiMentor.IdentidadeAcesso.Application.Queries.GetUserProfile;

public sealed record GetUserProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? AvatarUrl,
    string? Phone,
    DateOnly? BirthDate,
    string? Bio,
    IReadOnlyList<string> Roles,
    string Status,
    DateTime CreatedAt
);
