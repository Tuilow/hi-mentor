using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.BecomeCreator;

public sealed class BecomeCreatorCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow
) : IRequestHandler<BecomeCreatorCommand>
{
    public async Task Handle(BecomeCreatorCommand request, CancellationToken ct)
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

        await uow.SaveChangesAsync(ct);
    }
}
