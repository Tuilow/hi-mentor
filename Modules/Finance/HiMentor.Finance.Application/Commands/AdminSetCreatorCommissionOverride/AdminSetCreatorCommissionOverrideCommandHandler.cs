using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Commands.AdminSetCreatorCommissionOverride;

public sealed class AdminSetCreatorCommissionOverrideCommandHandler(
    ICreatorAsaasAccountRepository repository, IUnitOfWork uow
) : IRequestHandler<AdminSetCreatorCommissionOverrideCommand>
{
    public async Task Handle(AdminSetCreatorCommissionOverrideCommand request, CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(request.CreatorAsaasAccountId, ct)
            ?? throw new NotFoundException("Conta Asaas do criador", request.CreatorAsaasAccountId);

        account.SetCommissionOverride(request.Percentage);
        repository.Update(account);
        await uow.SaveChangesAsync(ct);
    }
}
