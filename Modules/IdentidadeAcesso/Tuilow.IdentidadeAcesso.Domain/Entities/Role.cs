using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.IdentidadeAcesso.Domain.Entities;

/// <summary>
/// Papel atribuível a um usuário (Student, Creator, Admin, ChannelMember, ...).
/// Ver Enums.RoleNames para os nomes padrão do sistema.
/// Um usuário pode possuir múltiplos roles simultaneamente — ver User.Roles.
/// </summary>
public sealed class Role : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Role() { }

    public static Role Create(string name, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Role { Name = name.Trim(), Description = description };
    }
}
