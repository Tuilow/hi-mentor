using HiMentor.Finance.Application.EventHandlers;
using HiMentor.Finance.Domain.Interfaces;
using MediatR;

namespace HiMentor.Finance.Application.Queries.GetCurrentPlatformFee;

public sealed class GetCurrentPlatformFeeQueryHandler(IPlatformFeeConfigurationRepository repository)
    : IRequestHandler<GetCurrentPlatformFeeQuery, PlatformFeeResponse>
{
    public async Task<PlatformFeeResponse> Handle(GetCurrentPlatformFeeQuery request, CancellationToken ct)
    {
        var config = await repository.GetActiveAsync(ct);
        return config is not null
            ? new PlatformFeeResponse(config.Percentage, config.EffectiveFrom)
            : new PlatformFeeResponse(CoursePurchaseConfirmedEventHandler.DefaultFeePercentage, DateTime.UtcNow);
    }
}
