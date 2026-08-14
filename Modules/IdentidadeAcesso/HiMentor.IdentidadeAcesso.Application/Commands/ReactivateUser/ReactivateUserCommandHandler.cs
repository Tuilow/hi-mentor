using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ReactivateUser;

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
