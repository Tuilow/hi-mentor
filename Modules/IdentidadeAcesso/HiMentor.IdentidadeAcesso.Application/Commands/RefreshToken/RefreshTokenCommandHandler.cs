using HiMentor.IdentidadeAcesso.Application.Common;
using HiMentor.IdentidadeAcesso.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<RefreshTokenCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByRefreshTokenAsync(request.Token, ct)
            ?? throw new UnauthorizedException("Refresh token inválido.");

        var existingToken = user.GetActiveRefreshToken(request.Token)
            ?? throw new UnauthorizedException("Refresh token expirado ou revogado.");

        var newRefreshTokenStr = jwtService.GenerateRefreshToken();
        var newExpires = DateTime.UtcNow.AddDays(30);

        existingToken.Revoke(newRefreshTokenStr);
        user.AddRefreshToken(newRefreshTokenStr, newExpires, request.IpAddress);

        // Bug encontrado em teste manual: NÃO chama userRepository.Update(user) — o usuário já
        // está rastreado pelo DbContext (veio de GetByRefreshTokenAsync na mesma unit of work).
        // Chamar Update() forçava o novo RefreshToken (Guid não-default, criado por
        // AddRefreshToken acima) para Modified em vez de Added, gerando UPDATE de 0 linhas →
        // DbUpdateConcurrencyException. Mesma causa já documentada em PromoteUserCommandHandler/
        // RemoveRoleCommandHandler para UserRoleAssignment — aqui pegou o RefreshToken.
        await uow.SaveChangesAsync(ct);

        var accessToken = jwtService.GenerateAccessToken(user);
        return new AuthTokens(accessToken, newRefreshTokenStr, DateTime.UtcNow.AddMinutes(15), newExpires);
    }
}
