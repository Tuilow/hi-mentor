using Tuilow.Domain.Common.Abstractions;
using Tuilow.Domain.Contexts.Identity.Enums;
using Tuilow.Domain.Contexts.Identity.Events;
using Tuilow.Domain.Contexts.Identity.ValueObjects;

namespace Tuilow.Domain.Contexts.Identity.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<SocialLogin> _socialLogins = [];
    private readonly List<UserRoleAssignment> _userRoles = [];

    public Email Email { get; private set; } = null!;
    public Password? Password { get; private set; }
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

    // Suporte a multi-role: um usuário pode ter vários roles simultâneos
    // (ex.: Student + Creator, Creator + ChannelMember, Admin + Creator).
    // Substituiu a antiga propriedade "Role" (enum único).
    public IReadOnlyCollection<UserRoleAssignment> UserRoleAssignments => _userRoles.AsReadOnly();
    public IEnumerable<Role> Roles => _userRoles.Select(ur => ur.Role);

    private User() { }

    // Factory: cadastro por e-mail e senha
    public static User Register(
        string email, string password, string firstName, string lastName, Role? defaultRole = null)
    {
        var user = new User
        {
            Email = Email.Create(email),
            Password = Password.CreateFromPlainText(password),
            EmailConfirmationToken = Guid.NewGuid().ToString("N")
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);

        if (defaultRole is not null)
            user.AssignRole(defaultRole);

        user.AddDomainEvent(new UserRegisteredDomainEvent(
            user.Id, email, firstName, user.EmailConfirmationToken));

        return user;
    }

    // Factory: login social (Google/Facebook)
    public static User RegisterFromSocialLogin(
        string email, string firstName, string lastName,
        string provider, string externalId, Role? defaultRole = null)
    {
        var user = new User
        {
            Email = Email.Create(email),
            Status = UserStatus.Active,
            EmailConfirmedAt = DateTime.UtcNow
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);
        user._socialLogins.Add(SocialLogin.Create(user.Id, provider, externalId, email));

        if (defaultRole is not null)
            user.AssignRole(defaultRole);

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

    public SocialLogin? AddSocialLogin(string provider, string externalId, string? email = null)
    {
        if (_socialLogins.Any(s => s.Provider == provider && s.ExternalId == externalId))
            return null;

        var socialLogin = SocialLogin.Create(Id, provider, externalId, email);
        _socialLogins.Add(socialLogin);
        Touch();
        return socialLogin;
    }

    /// <summary>
    /// Atribui um role ao usuário. Idempotente — retorna null se já estava atribuído.
    /// Quando o usuário já está sendo rastreado pelo DbContext (não é um Add novo),
    /// o vínculo retornado deve ser adicionado explicitamente via
    /// IUserRepository.AddUserRoleAssignmentAsync — mesmo padrão usado para
    /// RefreshToken/SocialLogin (Guid não-default gera UPDATE via DetectChanges em vez de INSERT).
    /// </summary>
    public UserRoleAssignment? AssignRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id)) return null;
        var assignment = UserRoleAssignment.Create(Id, role);
        _userRoles.Add(assignment);
        Touch();
        return assignment;
    }

    /// <summary>Remove um role do usuário, se estiver atribuído.</summary>
    public void RemoveRole(Guid roleId)
    {
        var assignment = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (assignment is null) return;
        _userRoles.Remove(assignment);
        Touch();
    }

    public bool HasRole(string roleName) =>
        _userRoles.Any(ur => string.Equals(ur.Role.Name, roleName, StringComparison.OrdinalIgnoreCase));

    public void Suspend()
    {
        Status = UserStatus.Suspended;
        Touch();
    }
}
