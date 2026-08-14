using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Enums;

namespace HiMentor.IdentidadeAcesso.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetBySocialLoginAsync(string provider, string externalId, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken ct = default);
    Task AddSocialLoginAsync(SocialLogin login, CancellationToken ct = default);
    Task AddUserRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken ct = default);
    Task<User?> GetByMagicLinkTokenAsync(string token, CancellationToken ct = default);
    Task AddMagicLinkTokenAsync(MagicLinkToken token, CancellationToken ct = default);

    /// <summary>
    /// Localiza o usuário pelo token de redefinição de senha (ver User.RequestPasswordReset) —
    /// o link do e-mail carrega só o token, sem UserId, então a busca precisa ser por token
    /// sozinho (mesmo padrão de GetByMagicLinkTokenAsync).
    /// </summary>
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Listagem paginada para o painel do dono da plataforma — inclui roles e refresh tokens
    /// (usados pela query handler para calcular o "último login" de cada usuário). Busca por
    /// e-mail ou nome/sobrenome (case-insensitive), filtro opcional por role e por status.
    /// </summary>
    Task<(IEnumerable<User> Items, int Total)> ListAllAsync(
        string? search, string? roleFilter, UserStatus? statusFilter,
        int page, int pageSize, CancellationToken ct = default);

    /// <summary>Contagens agregadas para a visão geral do painel do dono — evita carregar todos os usuários na memória só para contar.</summary>
    Task<UserCountsSnapshot> GetCountsSnapshotAsync(CancellationToken ct = default);
}

/// <summary>
/// Contagens usadas em <see cref="GetPlatformStatsQuery"/> — calculadas direto no banco
/// (COUNT), sem materializar as entidades.
/// </summary>
public sealed record UserCountsSnapshot(
    int TotalUsers,
    int ActiveUsers,
    int SuspendedUsers,
    int TotalCreators,
    int ActiveLast24h,
    int ActiveLast7d);
