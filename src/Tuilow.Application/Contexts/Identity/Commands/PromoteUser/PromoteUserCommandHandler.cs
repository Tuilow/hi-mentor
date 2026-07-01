using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.PromoteUser;

public sealed class PromoteUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IRequestHandler<PromoteUserCommand>
{
    public async Task Handle(PromoteUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        user.Promote(request.NewRole);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
