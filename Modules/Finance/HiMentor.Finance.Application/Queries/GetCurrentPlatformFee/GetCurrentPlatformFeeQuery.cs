using MediatR;

namespace HiMentor.Finance.Application.Queries.GetCurrentPlatformFee;

public sealed record GetCurrentPlatformFeeQuery : IRequest<PlatformFeeResponse>;

public sealed record PlatformFeeResponse(decimal Percentage, DateTime EffectiveFrom);
