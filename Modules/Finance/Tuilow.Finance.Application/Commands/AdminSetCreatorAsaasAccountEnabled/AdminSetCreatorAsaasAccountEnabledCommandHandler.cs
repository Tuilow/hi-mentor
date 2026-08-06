using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.AdminSetCreatorAsaasAccountEnabled;

public sealed class AdminSetCreatorAsaasAccountEnabledCommandHandler(
    ICreatorAsaasAccountRepository repository, IUnitOfWork uow
) : IRequestHandler<AdminSetCreatorAsaasAccountEnabledCommand>
{
    public async Task Handle(AdminSetCreatorAsaasAccountEnabledCommand request, CancellationToken ct)
    {
        var account = await repository.GetByIdAsync(request.CreatorAsaasAccountId, ct)
            ?? throw new NotFoundException("Conta Asaas do criador", request.CreatorAsaasAccountId);

        account.SetEnabledForSelling(request.Enabled);
        repository.Update(account);
        await uow.SaveChangesAsync(ct);
    }
}
