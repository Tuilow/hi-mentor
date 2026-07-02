using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>
/// Novo componente do SharedKernel: para quando um módulo só precisa LER uma entidade que
/// pertence a outro bounded context (ex.: Learning lendo Course do Catalog), sem poder escrevê-la —
/// evita que módulos peguem uma dependência de escrita indevida em agregados de outro contexto.
/// </summary>
public interface IReadOnlyRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
}
