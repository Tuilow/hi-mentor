using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;

namespace HiMentor.Finance.Tests.Fakes;

/// <summary>
/// Repositório fake em memória — evita depender de EF Core/Postgres nos testes de handler (sem
/// suíte de integração neste primeiro pass, ver Pendências no relatório final). Update/AddAsync
/// são "no-op de verdade" porque a mesma instância de CreatorAsaasSubaccount já está na lista —
/// mutações no agregado já refletem aqui, só existe para satisfazer a interface.
/// </summary>
public sealed class InMemoryCreatorAsaasSubaccountRepository : ICreatorAsaasSubaccountRepository
{
    private readonly List<CreatorAsaasSubaccount> _items = [];

    public int SaveChangesCallCount { get; private set; }

    public void RecordSaveChanges() => SaveChangesCallCount++;

    public Task<CreatorAsaasSubaccount?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<IEnumerable<CreatorAsaasSubaccount>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<CreatorAsaasSubaccount>>(_items);

    public Task<IEnumerable<CreatorAsaasSubaccount>> GetAllAsync(int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IEnumerable<CreatorAsaasSubaccount>>(_items.Skip(skip).Take(take).ToList());

    public Task<CreatorAsaasSubaccount?> GetByCreatorIdAsync(Guid creatorId, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.CreatorId == creatorId));

    public Task<CreatorAsaasSubaccount?> GetByAsaasAccountIdAsync(string asaasAccountId, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.AsaasAccountId == asaasAccountId));

    public Task<CreatorAsaasSubaccount?> GetByWebhookTokenHashAsync(string webhookTokenHash, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(x => x.WebhookTokenHash == webhookTokenHash));

    public Task AddAsync(CreatorAsaasSubaccount entity, CancellationToken ct = default)
    {
        if (!_items.Contains(entity))
            _items.Add(entity);
        return Task.CompletedTask;
    }

    public void Update(CreatorAsaasSubaccount entity)
    {
        // mesma instância em memória -- nada a fazer, ver comentário de classe.
    }

    public void Delete(CreatorAsaasSubaccount entity) => _items.Remove(entity);

    public int AddDocumentCallCount { get; private set; }

    public Task AddDocumentAsync(CreatorAsaasOnboardingDocument document, CancellationToken ct = default)
    {
        // Mesma instância em memória -- o documento já está em subaccount.Documents desde que
        // SyncDocuments o criou (ver comentário de classe: este fake não reproduz o rastreamento
        // Added/Modified real do EF Core, que é exatamente o que causava o bug corrigido nesta
        // sessão — só um teste com um DbContext real, ex.: provider InMemory/Sqlite, pegaria essa
        // classe de regressão). Serve para os testes verificarem QUANTAS vezes o handler chamou
        // este método, como proxy de "quantos documentos novos foram registrados como Added".
        AddDocumentCallCount++;
        return Task.CompletedTask;
    }
}
