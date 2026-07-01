using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Identity.Entities;

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }

    public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt, string? ip = null) =>
        new()
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedByIp = ip
        };

    public void Revoke(string? replacedByToken = null)
    {
        IsRevoked = true;
        ReplacedByToken = replacedByToken;
        Touch();
    }
}
