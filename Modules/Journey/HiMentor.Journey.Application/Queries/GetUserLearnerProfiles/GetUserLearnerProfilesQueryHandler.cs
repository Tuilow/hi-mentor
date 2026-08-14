using HiMentor.Journey.Domain.Interfaces;
using MediatR;

namespace HiMentor.Journey.Application.Queries.GetUserLearnerProfiles;

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
