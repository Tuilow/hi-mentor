using MediatR;

namespace Tuilow.Application.Contexts.Profiles.Queries.GetUserLearnerProfiles;

public sealed record GetUserLearnerProfilesQuery(Guid UserId) : IRequest<IEnumerable<LearnerProfileResponse>>;

public sealed record LearnerProfileResponse(
    Guid Id, string Name, string? Category,
    int? AgeMonths, string? PhotoUrl, string Level
);
