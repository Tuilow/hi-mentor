using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Profiles.Entities;
using Tuilow.Domain.Contexts.Profiles.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Profiles.Commands.RegisterLearnerProfile;

public sealed class RegisterLearnerProfileCommandHandler(
    ILearnerProfileRepository profileRepository, IUnitOfWork uow
) : IRequestHandler<RegisterLearnerProfileCommand, Guid>
{
    public async Task<Guid> Handle(RegisterLearnerProfileCommand request, CancellationToken ct)
    {
        var profile = LearnerProfile.Create(
            request.UserId, request.Name, request.Category, request.BirthDate);

        await profileRepository.AddAsync(profile, ct);
        await uow.SaveChangesAsync(ct);
        return profile.Id;
    }
}
