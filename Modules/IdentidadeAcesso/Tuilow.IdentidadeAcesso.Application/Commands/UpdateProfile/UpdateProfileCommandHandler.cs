using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.UpdateProfile;

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
