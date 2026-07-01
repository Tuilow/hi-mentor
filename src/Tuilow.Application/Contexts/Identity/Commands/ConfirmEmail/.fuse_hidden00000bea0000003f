using DogMaster.Application.Common.Exceptions;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Identity.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(
    IUserRepository userRepository, IUnitOfWork uow
) : IRequestHandler<ConfirmEmailCommand, bool>
{
    public async Task<bool> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("Usuário", request.UserId);

        user.ConfirmEmail(request.Token);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
