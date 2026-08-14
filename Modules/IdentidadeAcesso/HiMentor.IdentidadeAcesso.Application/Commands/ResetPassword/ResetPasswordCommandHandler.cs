using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository, IUnitOfWork uow
) : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByPasswordResetTokenAsync(request.Token, ct)
            ?? throw new BusinessException("Token de redefinição inválido ou expirado.");

        // User.ResetPassword revalida o token e a expiração (defesa em profundidade) e lança
        // InvalidOperationException com a mesma mensagem caso o token já tenha expirado —
        // ExceptionHandlingMiddleware converte ambos os casos numa resposta 422 com mensagem clara.
        user.ResetPassword(request.Token, request.NewPassword);
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
