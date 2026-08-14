using MediatR;

namespace HiMentor.Payout.Application.Commands.CompletePayout;

/// <summary>
/// Marca um saque aprovado como efetivamente pago. A transferência bancária/PIX em si é feita
/// manualmente (ou por integração futura) pela administração — este comando apenas registra
/// a conclusão e baixa definitivamente o valor reservado na carteira do criador.
/// </summary>
public sealed record CompletePayoutCommand(Guid PayoutRequestId, string? ExternalReference) : IRequest;
