using Tuilow.SharedKernel.Domain.Common;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.IdentidadeAcesso.Domain.Events;
using Tuilow.IdentidadeAcesso.Domain.ValueObjects;

namespace Tuilow.IdentidadeAcesso.Domain.Entities;

public sealed class User : AggregateRoot
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<SocialLogin> _socialLogins = [];
    private readonly List<UserRoleAssignment> _userRoles = [];
    private readonly List<MagicLinkToken> _magicLinkTokens = [];

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
    public IReadOnlyCollection<MagicLinkToken> MagicLinkTokens => _magicLinkTokens.AsReadOnly();

    // Suporte a multi-role: um usuário pode ter vários roles simultâneos
    // (ex.: Student + Creator, Creator + ChannelMember, Admin + Creator).
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
            // Código numérico de 6 dígitos (Sprint Item 4: dupla confirmação por e-mail antes de
            // permitir login) — antes era um GUID, mas um GUID não dá pra digitar manualmente;
            // o e-mail agora mostra este código pro usuário confirmar (ver EmailService.SendWelcomeAsync).
            EmailConfirmationToken = Random.Shared.Next(100000, 1000000).ToString()
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

    // Factory: checkout anônimo de compra de curso (sem senha, sem passar por cadastro) — o
    // acesso é feito só por Magic Link (ver IssueMagicLink/ConsumeMagicLink). A conta nasce
    // PendingConfirmation, com e-mail NÃO confirmado: a Asaas não verifica posse do e-mail
    // informado no checkout (só cria um "customer" com o que for enviado), então marcar como
    // confirmado aqui permitiria a qualquer visitante criar uma conta em nome de um terceiro só
    // sabendo o e-mail dele. A confirmação real (Status = Active, EmailConfirmedAt preenchido)
    // só acontece quando o Magic Link enviado a esse e-mail é de fato consumido — ver
    // ConsumeMagicLink, que é a prova real de posse do e-mail.
    public static User RegisterFromPurchase(
        string email, string firstName, string lastName, Role? defaultRole = null)
    {
        var user = new User
        {
            Email = Email.Create(email)
        };

        user.Profile = UserProfile.Create(user.Id, firstName, lastName);

        if (defaultRole is not null)
            user.AssignRole(defaultRole);

        user.AddDomainEvent(new UserRegisteredDomainEvent(user.Id, email, firstName, string.Empty));

        return user;
    }

    /// <summary>
    /// Emite um novo Magic Link (token opaco de uso único, 48h de validade por padrão) — chamado
    /// pelo módulo Learning quando o acesso a um curso é liberado, para o aluno entrar direto sem
    /// senha a partir do e-mail/WhatsApp.
    /// </summary>
    public MagicLinkToken IssueMagicLink(string token, TimeSpan? validFor = null)
    {
        var magicLink = MagicLinkToken.Create(Id, token, DateTime.UtcNow.Add(validFor ?? TimeSpan.FromHours(48)));
        _magicLinkTokens.Add(magicLink);
        return magicLink;
    }

    /// <summary>Valida e consome um Magic Link (uso único) — usado no login sem senha.</summary>
    public void ConsumeMagicLink(string token)
    {
        var link = _magicLinkTokens.SingleOrDefault(t => t.Token == token);
        if (link is null || !link.IsValid)
            throw new InvalidOperationException("Link de acesso inválido ou expirado.");

        link.Consume();

        // Consumir o Magic Link com sucesso é a prova real de posse do e-mail — a pessoa recebeu
        // o link no e-mail informado no checkout (ou por WhatsApp, encaminhado do mesmo e-mail) e
        // clicou nele. É só aqui, e não na criação da conta pelo checkout anônimo (ver
        // RegisterFromPurchase), que o e-mail deve ser considerado confirmado. Não regride uma
        // conta já confirmada por outro meio (senha, login social).
        if (Status == UserStatus.PendingConfirmation)
        {
            Status = UserStatus.Active;
            EmailConfirmedAt = DateTime.UtcNow;
        }

        Touch();
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

        // Achado em teste manual: contas criadas por compra (RegisterFromPurchase) nascem
        // PendingConfirmation e só saem desse status consumindo o Magic Link enviado por
        // e-mail (ver ConsumeMagicLink) -- que expira em 48h e é de uso único. Sem este
        // bloco, quem perdia essa janela ficava num beco sem saída: "esqueci minha senha"
        // deixava definir uma senha nova, mas o login continuava bloqueado com "Confirme seu
        // e-mail..." (ver LoginUserCommandHandler), sem nenhum jeito de sair dali sozinho.
        // Completar a redefinição de senha só é possível recebendo e clicando o link enviado
        // pro mesmo e-mail da conta — é prova de posse equivalente à do Magic Link, então
        // conta como confirmação aqui também. Não regride uma conta já ativa por outro meio.
        if (Status == UserStatus.PendingConfirmation)
        {
            Status = UserStatus.Active;
            EmailConfirmedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Bug reportado pelo usuário ("sessão caindo sozinha"): este método era chamado por TODO
    /// fluxo que emite um refresh token — login, login com Google, magic link, become-creator e
    /// a própria renovação de token (RefreshTokenCommandHandler) — e revogava indiscriminadamente
    /// QUALQUER outro refresh token ainda ativo do usuário, não só o que estava sendo trocado.
    /// Resultado: logar no celular derrubava o computador (e vice-versa), e até duas abas do
    /// mesmo navegador podiam se derrubar mutuamente na próxima renovação silenciosa (a cada
    /// ~15min, ver Jwt:ExpirationMinutes) — sem nenhum aviso, exatamente como "a sessão cai
    /// sozinha". Corrigido para múltiplas sessões simultâneas por usuário (comportamento padrão
    /// de apps como Gmail/Netflix): cada dispositivo/aba mantém seu próprio refresh token,
    /// independente dos demais. A troca do token específico sendo renovado continua acontecendo
    /// normalmente — ver RefreshTokenCommandHandler, que já chama `existingToken.Revoke(...)`
    /// no token exato antes de chamar este método, então a rotação daquela sessão específica não
    /// muda em nada.
    /// </summary>
    public RefreshToken AddRefreshToken(string token, DateTime expiresAt, string? ip = null)
    {
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

    /// <summary>
    /// Suspende a conta (bloqueia login) e revoga qualquer sessão ativa — sem revogar os
    /// refresh tokens, a pessoa continuaria logada até o token expirar naturalmente.
    /// </summary>
    public void Suspend()
    {
        Status = UserStatus.Suspended;
        foreach (var rt in _refreshTokens.Where(t => t.IsActive))
            rt.Revoke();
        Touch();
    }

    /// <summary>Reverte uma suspensão (ou reativa uma conta previamente excluída) — volta a poder logar.</summary>
    public void Reactivate()
    {
        Status = UserStatus.Active;
        Touch();
    }

    /// <summary>
    /// Exclusão via painel do dono da plataforma: soft-delete (preserva a linha no banco para
    /// histórico financeiro/fiscal — ver PlatformFeeConfiguration, comissões, compras já feitas),
    /// bloqueia login e revoga qualquer sessão ativa. Cursos (arquivamento) continuam tratados por
    /// DeleteUserCommandHandler (Catalog, Domain-to-Domain). Vídeos (achado M11) não são mais
    /// apagados de dentro deste fluxo — o evento abaixo delega isso ao módulo Streaming.
    /// </summary>
    public void MarkDeleted()
    {
        Status = UserStatus.Deleted;
        foreach (var rt in _refreshTokens.Where(t => t.IsActive))
            rt.Revoke();
        Touch();

        // Achado M11 da auditoria de arquitetura: consumido por Streaming.Application.EventHandlers.
        // UserDeletedEventHandler, que apaga os vídeos dos cursos deste criador (registro local +
        // Cloudflare Stream) — sem isso, DeleteUserCommandHandler precisava referenciar
        // Tuilow.Streaming.Application diretamente, a única referência Application-to-Application
        // entre módulos do repositório.
        AddDomainEvent(new UserDeletedDomainEvent(Id));
    }
}
