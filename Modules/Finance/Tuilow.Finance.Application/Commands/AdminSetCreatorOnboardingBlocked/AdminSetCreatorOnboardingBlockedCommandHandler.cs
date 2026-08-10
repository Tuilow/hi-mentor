using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.AdminSetCreatorOnboardingBlocked;

public sealed class AdminSetCreatorOnboardingBlockedCommandHandler(
    ICreatorAsaasSubaccountRepository repository, IUnitOfWork uow
) : IRequestHandler<AdminSetCreatorOnboardingBlockedCommand>
{
    public async Task Handle(AdminSetCreatorOnboardingBlockedCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByIdAsync(request.CreatorAsaasSubaccountId, ct)
            ?? throw new NotFoundException("Conta financeira do criador", request.CreatorAsaasSubaccountId);

        if (request.Blocked)
        {
            subaccount.Block(request.Reason ?? "Bloqueado pelo administrador.");
        }
        else
        {
            // ApprovedAt só é preenchido quando MarkApproved já rodou alguma vez -- usamos isso
            // pra saber se devolvemos pra Approved ou pra UnderReview ao desbloquear.
            var wasApprovedBefore = subaccount.ApprovedAt is not null && subaccount.Status != CreatorOnboardingStatus.Rejected;
            subaccount.Unblock(wasApprovedBefore);
        }

        repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);
    }
}
