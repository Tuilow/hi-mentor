namespace HiMentor.SharedKernel.Application.Interfaces;

/// <summary>
/// Reaproveitado de HiMentor.Application.Common.Interfaces.ICurrentUserService — movido para o
/// SharedKernel porque todo módulo precisa saber "quem é o usuário autenticado", não só Identity.
/// </summary>
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
