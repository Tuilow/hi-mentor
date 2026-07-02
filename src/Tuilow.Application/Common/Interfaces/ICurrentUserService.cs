namespace Tuilow.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    /// <summary>Primeiro role do usuário (compatibilidade). Prefira <see cref="Roles"/> em cenários multi-role.</summary>
    string? Role { get; }
    /// <summary>Todos os roles do usuário autenticado (suporte a multi-role).</summary>
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
