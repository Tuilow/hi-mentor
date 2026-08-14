using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiMentor.Finance.Infrastructure.Repositories;

public sealed class ProcessedAsaasAccountEventRepository(DbContext context) : IProcessedAsaasAccountEventRepository
{
    public async Task<bool> ExistsAsync(string asaasEventId, CancellationToken ct = default) =>
        await context.Set<ProcessedAsaasAccountEvent>().AnyAsync(e => e.AsaasEventId == asaasEventId, ct);

    public async Task AddAsync(ProcessedAsaasAccountEvent entity, CancellationToken ct = default) =>
        await context.Set<ProcessedAsaasAccountEvent>().AddAsync(entity, ct);
}
