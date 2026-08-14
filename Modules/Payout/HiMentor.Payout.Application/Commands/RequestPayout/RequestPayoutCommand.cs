using MediatR;

namespace HiMentor.Payout.Application.Commands.RequestPayout;

/// <summary>
/// Solicita saque do saldo disponível do criador. Se <see cref="Amount"/> não for informado,
/// solicita o saldo disponível integral no momento do pedido.
/// </summary>
public sealed record RequestPayoutCommand(Guid CreatorId, decimal? Amount) : IRequest<Guid>;
