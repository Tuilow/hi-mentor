using DogMaster.Domain.Common.Abstractions;
using DogMaster.Domain.Contexts.Identity.Enums;
using DogMaster.Domain.Contexts.Identity.Events;
using DogMaster.Domain.Contexts.Identity.ValueObjects;

namespace DogMaster.Domain.Contexts.Identity.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<SocialLogin> _socialLogins = [];

    public Email Email { get; private set; } = null!;
    public Password? Password { get; private set; }
    public UserRole Role { get; private set; } = UserRole.Student;
    public UserStatus Status { get; private set; } = UserStatus.PendingConfirmation;
    public DateTime? EmailConfirmedAt { get; private set; }
    public string? EmailConfirmationToken { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresAt { get; private set; }

    public bool IsEmailConfirmed => EmailConfirmedAt.HasValue;
    public DateTime? PasswordResetTokenExpiry => PasswordResetTokenExpiresAt;

    public UserProfile Profile { get; private set; } = null!;
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();
    public IReadOnlyCollection<SocialLogin> SocialLogins => _socialLogins.AsReadOnly();

    private User() { }

    // Factory: cadastro por e-mail e senha
    public static User Register(string email, string password, string firstName, string lastName)
    {
        var user = new User
        {
            Email = Email.Create(email),
            Password = Password.CreateFromPlainText(password),
            EmailConfirmationToken = Guid.NewGuid().ToString("N")
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);

        user.AddDomainEvent(new UserRegisteredDomainEvent(
            user.Id, email, firstName, user.EmailConfirmationToken));

        return user;
    }

    // Factory: login social (Google/Facebook)
    public static User RegisterFromSocialLogin(
        string email, string firstName, string lastName,
        string provider, string externalId)
    {
        var user = new User
        {
            Email = Email.Create(email),
            Status = UserStatus.Active,
            EmailConfirmedAt = DateTime.UtcNow
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);
        user._socialLogins.Add(SocialLogin.Create(user.Id, provider, externalId, email));

        user.AddDomainEvent(new UserRegisteredDomainEvent(
            user.Id, email, firstName, string.Empty));

        return user;
    }

    public void ConfirmEmail(string token)
    {
        if (Status == UserStatus.Active) return;

        if (EmailConfirmationToken != token)
            throw new InvalidOperationException("Token de confirmação inválido.");

        Status = UserStatus.Active;
        EmailConfirmedAt = DateTime.UtcNow;
        EmailConfirmationToken = null;
        Touch();

        AddDomainEvent(new UserEmailConfirmedDomainEvent(Id));
    }

    public bool ValidatePassword(string plainText)
    {
        if (Password is null) return false;
        return Password.Verify(plainText);
    }

    public void ChangePassword(string newPassword)
    {
        Password = Password.CreateFromPlainText(newPassword);
        PasswordResetToken = null;
        PasswordResetTokenExpiresAt = null;
        Touch();
    }

    public string RequestPasswordReset()
    {
        PasswordResetToken = Guid.NewGuid().ToString("N");
        PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        Touch();

        AddDomainEvent(new UserPasswordResetRequestedDomainEvent(Id, Email.Value, PasswordResetToken));
        return PasswordResetToken;
    }

    public void ResetPassword(string token, string newPassword)
    {
        if (PasswordResetToken != token || PasswordResetTokenExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Token de redefinição inválido ou expirado.");

        ChangePassword(newPassword);
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAt, string? ip = null)
    {
        // Revoga tokens anteriores ainda ativos
        foreach (var rt in _refreshTokens.Where(t => t.IsActive))
            rt.Revoke(token);

        var newToken = RefreshToken.Create(Id, token, expiresAt, ip);
        _refreshTokens.Add(newToken);
        return newToken;
    }

    public RefreshToken? GetActiveRefreshToken(string token) =>
        _refreshTokens.SingleOrDefault(t => t.Token == token && t.IsActive);

    public void AddSocialLogin(string provider, string externalId, string? email = null)
    {
        if (_socialLogins.Any(s => s.Provider == provider && s.ExternalId == externalId))
            return;

        _socialLogins.Add(SocialLogin.Create(Id, provider, externalId, email));
        Touch();
    }

    public void Promote(UserRole role)
    {
        Role = role;
        Touch();
    }

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        Touch();
    }
}
