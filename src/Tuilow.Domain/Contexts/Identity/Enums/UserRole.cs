namespace Tuilow.Domain.Contexts.Identity.Enums;

/// <summary>
/// Nomes padrão de roles do sistema. Substitui o antigo enum UserRole de role único —
/// agora um usuário pode ter múltiplos roles simultâneos (ver User.Roles / Entities.Role).
/// Nota: idealmente este arquivo se chamaria RoleNames.cs; mantido com este nome por
/// limitação de exclusão/rename de arquivo no ambiente em que foi escrito.
/// </summary>
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
