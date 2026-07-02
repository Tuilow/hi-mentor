namespace Tuilow.IdentidadeAcesso.Domain.Enums;

/// <summary>Nomes padrão de roles do sistema. Um usuário pode ter múltiplos roles simultâneos.</summary>
public static class RoleNames
{
    public const string Student = "Student";
    public const string Creator = "Creator";
    public const string Admin = "Admin";
    public const string ChannelMember = "ChannelMember";

    public static readonly IReadOnlyList<string> All = [Student, Creator, Admin, ChannelMember];

    public static bool IsValid(string name) =>
        All.Contains(name, StringComparer.OrdinalIgnoreCase);
}
