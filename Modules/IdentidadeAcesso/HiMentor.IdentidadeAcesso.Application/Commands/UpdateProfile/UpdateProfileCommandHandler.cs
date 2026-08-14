using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IRequestHandler<UpdateProfileCommand>
{
    public async Task Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("Usuário", request.UserId);

        user.Profile.Update(request.FirstName, request.LastName, request.Phone, request.BirthDate, request.Bio);
        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.Profile.SetAvatar(request.AvatarUrl);

        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
