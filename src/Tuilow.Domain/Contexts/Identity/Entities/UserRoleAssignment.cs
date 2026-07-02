using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Identity.Entities;

/// <summary>
/// Vínculo N:N entre um usuário e um role. Permite múltiplos roles simultâneos
/// por usuário (ex.: Student + Creator, Creator + ChannelMember, Admin + Creator).
/// </summary>
public sealed class UserRoleAssignment : Entity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    private UserRoleAssignment() { }

    public static UserRoleAssignment Create(Guid userId, Role role) =>
        new()
        {
            UserId = userId,
            RoleId = role.Id,
            Role = role
        };
}
