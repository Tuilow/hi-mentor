using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.IdentidadeAcesso.Domain.Entities;

/// <summary>
/// Vínculo do usuário com um provider de login social. O campo Provider é uma string livre
/// hoje (Google, ...) — ver nota em IdentidadeAcesso.Application sobre abstração LoginProvider
/// para preparar a arquitetura para múltiplos providers (Microsoft, LinkedIn, Apple, Facebook, GitHub).
/// </summary>
public sealed class SocialLogin : Entity
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;   // Google, Facebook, ...
    public string ExternalId { get; private set; } = string.Empty;
    public string? ExternalEmail { get; private set; }

    private SocialLogin() { }

    public static SocialLogin Create(Guid userId, string provider, string externalId, string? externalEmail = null) =>
        new()
        {
            UserId = userId,
            Provider = provider,
            ExternalId = externalId,
            ExternalEmail = externalEmail
        };
}
