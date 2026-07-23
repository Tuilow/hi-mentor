namespace Tuilow.IdentidadeAcesso.Application.Queries.ListUsers;

public sealed record UserSummaryResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
