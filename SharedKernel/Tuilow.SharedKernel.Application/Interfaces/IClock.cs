namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>
/// Novo componente do SharedKernel: abstrai DateTime.UtcNow para permitir controle em testes
/// (o código atual usa DateTime.UtcNow diretamente nas entidades — migração gradual recomendada).
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
