using Tuilow.SharedKernel.Application.Interfaces;

namespace Tuilow.Finance.Tests.Fakes;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(0);
    }
}
