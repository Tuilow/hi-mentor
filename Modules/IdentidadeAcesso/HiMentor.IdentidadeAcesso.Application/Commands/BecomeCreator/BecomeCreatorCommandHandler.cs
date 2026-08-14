using HiMentor.IdentidadeAcesso.Application.Common;
using HiMentor.IdentidadeAcesso.Application.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.BecomeCreator;

public sealed class BecomeCreatorCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow,
    IJwtService jwtService
) : IRequestHandler<BecomeCreatorCommand, AuthTokens>
{
    public async Task<AuthTokens> Handle(BecomeCreatorCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("Usuário", request.UserId);

        var creatorRole = await roleRepository.GetByNameAsync(RoleNames.Creator, ct)
            ?? throw new InvalidOperationException("Role Creator não encontrado — verifique o seed de roles.");

        // NÃO chama userRepository.Update(user) — mesmo motivo documentado em
        // PromoteUserCommandHandler: o usuário já está rastreado pela mesma unit of work
        // (veio de GetByIdAsync), e chamar Update() marcaria o novo UserRoleAssignment como
        // Modified em vez de Added, gerando UPDATE de 0 linhas.
        var assignment = user.AssignRole(creatorRole);
        if (assignment is not null)
            await userRepository.AddUserRoleAssignmentAsync(assignment, ct);

        // Sem refresh token ativo (ex.: expirou) é o único caso em que precisamos emitir um novo
        // — normalmente o front só precisa de um access token atualizado (claims de role mudaram),
        // então evitamos rotacionar o refresh token à toa (ele é de uso único; rotacionar sem
        // necessidade só criaria mais uma chance de invalidar um token que outra aba/request
        // ainda esteja usando).
        var activeRefreshToken = user.RefreshTokens
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.ExpiresAt)
            .FirstOrDefault();

        string refreshTokenStr;
        DateTime refreshTokenExpires;
        if (activeRefreshToken is not null)
        {
            refreshTokenStr = activeRefreshToken.Token;
            refreshTokenExpires = activeRefreshToken.ExpiresAt;
        }
        else
        {
            refreshTokenStr = jwtService.GenerateRefreshToken();
            refreshTokenExpires = DateTime.UtcNow.AddDays(30);
            user.AddRefreshToken(refreshTokenStr, refreshTokenExpires);
        }

        await uow.SaveChangesAsync(ct);

        // Gerado depois do AssignRole (em memória, sem precisar recarregar o usuário) — o claim
        // de role "Creator" já reflete em user.Roles nesse ponto.
        var accessToken = jwtService.GenerateAccessToken(user);
        return new AuthTokens(accessToken, refreshTokenStr, DateTime.UtcNow.AddMinutes(15), refreshTokenExpires);
    }
}
