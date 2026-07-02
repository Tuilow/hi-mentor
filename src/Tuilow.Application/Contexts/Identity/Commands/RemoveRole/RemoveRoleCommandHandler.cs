using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Identity.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Identity.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IUnitOfWork uow
) : IRequestHandler<RemoveRoleCommand>
{
    public async Task Handle(RemoveRoleCommand request, CancellationToken ct)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, ct)
            ?? throw new NotFoundException("Usuário", request.TargetUserId);

        var role = await roleRepository.GetByNameAsync(request.RoleName, ct)
            ?? throw new NotFoundException("Role", request.RoleName);

        // NÃO chama userRepository.Update(user) — o usuário já está rastreado pelo
        // DbContext; remover um item da coleção rastreada já é detectado automaticamente
        // pelo DetectChanges como exclusão do vínculo.
        user.RemoveRole(role.Id);
        await uow.SaveChangesAsync(ct);
    }
}
