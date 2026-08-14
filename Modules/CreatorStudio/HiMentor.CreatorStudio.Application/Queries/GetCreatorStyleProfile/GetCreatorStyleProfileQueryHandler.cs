using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetCreatorStyleProfile;

public sealed class GetCreatorStyleProfileQueryHandler(
    ICreatorStyleProfileRepository profileRepository,
    ILessonScriptRepository scriptRepository
) : IRequestHandler<GetCreatorStyleProfileQuery, CreatorStyleProfileResponse?>
{
    public async Task<CreatorStyleProfileResponse?> Handle(GetCreatorStyleProfileQuery request, CancellationToken ct)
    {
        var profile = await profileRepository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (profile is null) return null;

        var recordedCount = await scriptRepository.CountRecordedByCreatorAsync(request.CreatorId, ct);

        return new CreatorStyleProfileResponse(
            profile.Id, profile.Niche, profile.TargetAudience, profile.Objective, profile.Level,
            recordedCount, CreatorStyleProfile.ScriptsRequiredForClone,
            recordedCount >= CreatorStyleProfile.ScriptsRequiredForClone);
    }
}
