using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>Reaproveitado de Tuilow.Domain.Common.Interfaces.IRepository — movido para o SharedKernel.</summary>
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
