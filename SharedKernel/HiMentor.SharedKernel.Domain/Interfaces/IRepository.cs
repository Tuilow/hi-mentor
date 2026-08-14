using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.SharedKernel.Domain.Interfaces;

/// <summary>
/// Reaproveitado de HiMentor.Domain.Common.Interfaces.IRepository — movido para o SharedKernel.
/// Fica no Domain (não no Application) para preservar a regra de dependência do Clean
/// Architecture: o contrato de persistência de um agregado é parte do próprio domínio.
/// </summary>
public interface IRepository<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
