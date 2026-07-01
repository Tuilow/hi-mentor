using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.ConfirmEmail;

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
