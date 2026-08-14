using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Commands.AdminSetCreatorAsaasAccountEnabled;

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
