using MediatR;

namespace Tuilow.Journey.Application.Queries.GetUserLearnerProfiles;

public sealed record GetUserLearnerProfilesQuery(Guid UserId) : IRequest<IEnumerable<LearnerProfileResponse>>;

public sealed record LearnerProfileResponse(
    Guid Id, string Name, string? Category,
    int? AgeMonths, string? PhotoUrl, string Level
);
