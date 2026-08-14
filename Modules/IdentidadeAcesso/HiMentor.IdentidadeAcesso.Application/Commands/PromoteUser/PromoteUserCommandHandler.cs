using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.PromoteUser;

public sealed class PromoteUserCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow
) : IRequestHandler<PromoteUserCommand>
{
    public async Task Handle(PromoteUserCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        var role = await roleRepository.GetByNameAsync(request.RoleName, ct)
            ?? throw new NotFoundException("Role", request.RoleName);

        // NÃO chama userRepository.Update(user) — o usuário já está rastreado pelo
        // DbContext (veio de GetByIdAsync na mesma unit of work). Chamar Update()
        // forçaria o novo UserRoleAssignment (Guid não-default) para Modified em vez
        // de Added, gerando UPDATE de 0 linhas → DbUpdateConcurrencyException.
        var assignment = user.AssignRole(role);
        if (assignment is not null)
            await userRepository.AddUserRoleAssignmentAsync(assignment, ct);

        await uow.SaveChangesAsync(ct);
    }
}
