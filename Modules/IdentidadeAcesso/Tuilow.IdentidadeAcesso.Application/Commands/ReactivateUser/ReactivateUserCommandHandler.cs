using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ReactivateUser;

public sealed class ReactivateUserCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(ReactivateUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        user.Reactivate();
        await uow.SaveChangesAsync(ct);
    }
}
