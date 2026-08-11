using Tuilow.SharedKernel.Application.Interfaces;

namespace Tuilow.Finance.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }
    public int TrySaveChangesCallCount { get; private set; }
    public int ClearTrackingCallCount { get; private set; }

    /// <summary>
    /// Configurável pelos testes: quantas das próximas chamadas a TrySaveChangesAsync devem
    /// simular "perdeu a corrida de inserção concorrente" (devolve false, como o AppDbContext
    /// real faz ao capturar uma violação de restrição única do Postgres) antes de finalmente ter
    /// sucesso. 0 (padrão) = sempre sucesso de primeira.
    /// </summary>
    public int SimulatedConflictsBeforeSuccess { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(0);
    }

    public Task<bool> TrySaveChangesAsync(CancellationToken ct = default)
    {
        TrySaveChangesCallCount++;
        SaveChangesCallCount++;

        if (SimulatedConflictsBeforeSuccess > 0)
        {
            SimulatedConflictsBeforeSuccess--;
            ClearTrackingCallCount++;
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
