using HiMentor.Finance.Domain.Enums;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Queries.GetMyAsaasAccountStatus;

public sealed class GetMyAsaasAccountStatusQueryHandler(ICreatorAsaasAccountRepository repository)
    : IRequestHandler<GetMyAsaasAccountStatusQuery, CreatorAsaasAccountStatusResponse>
{
    public async Task<CreatorAsaasAccountStatusResponse> Handle(GetMyAsaasAccountStatusQuery request, CancellationToken ct)
    {
        var account = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (account is null)
            return new CreatorAsaasAccountStatusResponse(false, CreatorAsaasAccountStatus.NotConnected.ToString(), false, null, null, null, null);

        return new CreatorAsaasAccountStatusResponse(
            true, account.Status.ToString(), account.CanSell, account.WalletId,
            account.CommissionOverridePercentage, account.LastValidatedAt, account.LastValidationError);
    }
}
