using MediatR;

namespace HiMentor.Finance.Application.Commands.UpdatePlatformFee;

/// <summary>
/// Define um novo percentual de comissão da plataforma (uso administrativo). A configuração
/// anterior é desativada (não removida — preserva histórico para auditoria) e a nova passa a
/// valer para toda venda confirmada a partir de agora.
/// </summary>
public sealed record UpdatePlatformFeeCommand(
    decimal Percentage, Guid AdminUserId, string? Notes = null
) : IRequest<Guid>;
