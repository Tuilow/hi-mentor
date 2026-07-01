using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Identity.Entities;

public sealed class SocialLogin : Entity
{
    public Guid UserId { get; private set; }
    public string Provider { get; private set; } = string.Empty;   // Google, Facebook
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
