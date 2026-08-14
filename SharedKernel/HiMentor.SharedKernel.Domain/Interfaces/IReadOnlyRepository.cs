namespace HiMentor.SharedKernel.Domain.Interfaces;

/// <summary>
/// Para quando um módulo só precisa LER uma entidade que pertence a outro bounded context
/// (ex.: Learning lendo Course do Catalog), sem poder escrevê-la — evita que módulos peguem
/// uma dependência de escrita indevida em agregados de outro contexto.
/// </summary>
public interface IReadOnlyRepository<T> where T : Common.Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
}
