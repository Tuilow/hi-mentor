using HiMentor.Finance.Domain.Entities;

namespace HiMentor.Finance.Domain.Interfaces;

/// <summary>Dedupe de webhooks de status de conta (ver ProcessedAsaasAccountEvent) — não segue IRepository&lt;AggregateRoot&gt; porque a entidade não é um agregado com ciclo de vida próprio, só um registro de "já processei isto".</summary>
public interface IProcessedAsaasAccountEventRepository
{
    Task<bool> ExistsAsync(string asaasEventId, CancellationToken ct = default);
    Task AddAsync(ProcessedAsaasAccountEvent entity, CancellationToken ct = default);
}
