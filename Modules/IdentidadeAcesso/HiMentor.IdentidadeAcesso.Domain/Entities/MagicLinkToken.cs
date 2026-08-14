using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.IdentidadeAcesso.Domain.Entities;

/// <summary>
/// Token de uso único para login sem senha ("Magic Link") — emitido automaticamente após a
/// confirmação de uma compra de curso, para o aluno entrar direto na área do curso a partir do
/// e-mail/WhatsApp, sem precisar criar ou lembrar senha. Vida curta (48h) e uso único, diferente
/// de <see cref="RefreshToken"/> (30 dias, reutilizável até expirar/ser revogado).
/// </summary>
public sealed class MagicLinkToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    public bool IsValid => ConsumedAt is null && ExpiresAt > DateTime.UtcNow;

    private MagicLinkToken() { }

    public static MagicLinkToken Create(Guid userId, string token, DateTime expiresAt) =>
        new() { UserId = userId, Token = token, ExpiresAt = expiresAt };

    public void Consume() => ConsumedAt = DateTime.UtcNow;
}
