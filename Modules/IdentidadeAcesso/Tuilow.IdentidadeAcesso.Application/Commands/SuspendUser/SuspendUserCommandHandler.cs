using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.SuspendUser;

public sealed class SuspendUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IRequestHandler<SuspendUserCommand>
{
    public async Task Handle(SuspendUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        user.Suspend();
        await uow.SaveChangesAsync(ct);
    }
}
