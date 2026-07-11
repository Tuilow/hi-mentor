namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>Reaproveitado de Tuilow.Domain.Common.Interfaces.IUnitOfWork — movido para o SharedKernel.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
