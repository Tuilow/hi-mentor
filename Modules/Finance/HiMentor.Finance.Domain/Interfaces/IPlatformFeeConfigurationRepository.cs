using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Finance.Domain.Entities;

namespace HiMentor.Finance.Domain.Interfaces;

public interface IPlatformFeeConfigurationRepository : IRepository<PlatformFeeConfiguration>
{
    /// <summary>Configuração de percentual ativa no momento (usada para calcular a comissão de cada venda).</summary>
    Task<PlatformFeeConfiguration?> GetActiveAsync(CancellationToken ct = default);

    Task<IEnumerable<PlatformFeeConfiguration>> GetHistoryAsync(CancellationToken ct = default);
}
