using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;

namespace HiMentor.Finance.Tests.Fakes;

public sealed class InMemoryProcessedAsaasAccountEventRepository : IProcessedAsaasAccountEventRepository
{
    private readonly List<ProcessedAsaasAccountEvent> _items = [];

    public IReadOnlyList<ProcessedAsaasAccountEvent> Items => _items.AsReadOnly();

    public Task<bool> ExistsAsync(string asaasEventId, CancellationToken ct = default) =>
        Task.FromResult(_items.Any(x => x.AsaasEventId == asaasEventId));

    public Task AddAsync(ProcessedAsaasAccountEvent entity, CancellationToken ct = default)
    {
        _items.Add(entity);
        return Task.CompletedTask;
    }
}
