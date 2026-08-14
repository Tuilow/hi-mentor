using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler(
    IUserRepository userRepository, IUnitOfWork uow
) : IRequestHandler<ConfirmEmailCommand, bool>
{
    public async Task<bool> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, ct)
            ?? throw new NotFoundException("Usuário", request.Email);

        user.ConfirmEmail(request.Code);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
        return true;
    }
}
