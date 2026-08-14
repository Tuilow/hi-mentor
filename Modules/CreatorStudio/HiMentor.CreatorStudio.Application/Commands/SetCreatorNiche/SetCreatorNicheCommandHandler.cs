using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.SetCreatorNiche;

public sealed class SetCreatorNicheCommandHandler(
    ICreatorStyleProfileRepository profileRepository,
    IUnitOfWork uow
) : IRequestHandler<SetCreatorNicheCommand, Guid>
{
    public async Task<Guid> Handle(SetCreatorNicheCommand request, CancellationToken ct)
    {
        var profile = await profileRepository.GetByCreatorIdAsync(request.CreatorId, ct);

        if (profile is null)
        {
            profile = CreatorStyleProfile.Create(
                request.CreatorId, request.Niche, request.TargetAudience, request.Objective, request.Level);
            await profileRepository.AddAsync(profile, ct);
        }
        else
        {
            profile.Update(request.Niche, request.TargetAudience, request.Objective, request.Level);
            profileRepository.Update(profile);
        }

        await uow.SaveChangesAsync(ct);
        return profile.Id;
    }
}
