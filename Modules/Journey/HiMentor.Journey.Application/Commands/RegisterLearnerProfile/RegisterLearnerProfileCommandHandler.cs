using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Journey.Domain.Entities;
using HiMentor.Journey.Domain.Interfaces;
using MediatR;

namespace HiMentor.Journey.Application.Commands.RegisterLearnerProfile;

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
