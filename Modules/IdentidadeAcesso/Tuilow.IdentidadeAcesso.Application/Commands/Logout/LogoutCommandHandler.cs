using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.Logout;

public sealed class LogoutCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow
) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.RefreshToken)) return;

        var user = await userRepository.GetByRefreshTokenAsync(request.RefreshToken, ct);
        if (user is null) return;

        var token = user.GetActiveRefreshToken(request.RefreshToken);
        if (token is null) return;

        token.Revoke();
        userRepository.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
