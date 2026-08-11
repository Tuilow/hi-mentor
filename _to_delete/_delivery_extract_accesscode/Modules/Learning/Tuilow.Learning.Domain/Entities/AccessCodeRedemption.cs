using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Entities;

/// <summary>
/// Um resgate de AccessCode por um aluno específico — dá o rastro "quem ativou e quando" (pedido
/// explícito da modelagem) e, por ter índice único (AccessCodeId, UserId), impede o mesmo aluno
/// de resgatar o mesmo código duas vezes (ver AccessCodeConfiguration/AccessCodeRedemptionConfiguration).
/// </summary>
public sealed class AccessCodeRedemption : Entity
{
    public Guid AccessCodeId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime RedeemedAt { get; private set; }

    private AccessCodeRedemption() { }

    public static AccessCodeRedemption Create(Guid accessCodeId, Guid userId) =>
        new() { AccessCodeId = accessCodeId, UserId = userId, RedeemedAt = DateTime.UtcNow };
}
