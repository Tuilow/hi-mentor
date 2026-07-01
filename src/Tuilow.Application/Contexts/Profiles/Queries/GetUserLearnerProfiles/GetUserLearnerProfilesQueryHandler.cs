using Tuilow.Domain.Contexts.Profiles.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Profiles.Queries.GetUserLearnerProfiles;

public sealed class GetUserLearnerProfilesQueryHandler(ILearnerProfileRepository profileRepository)
    : IRequestHandler<GetUserLearnerProfilesQuery, IEnumerable<LearnerProfileResponse>>
{
    public async Task<IEnumerable<LearnerProfileResponse>> Handle(GetUserLearnerProfilesQuery request, CancellationToken ct)
    {
        var profiles = await profileRepository.GetByUserAsync(request.UserId, ct);
        return profiles.Select(p => new LearnerProfileResponse(
            p.Id, p.Name, p.Category, p.AgeMonths, p.PhotoUrl, p.Level.ToString()));
    }
}
