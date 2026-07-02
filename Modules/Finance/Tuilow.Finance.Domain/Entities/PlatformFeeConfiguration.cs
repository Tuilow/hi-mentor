using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Configuração administrativa do percentual de comissão retido pela plataforma sobre cada
/// venda de curso. Nunca fixar o percentual em código — toda venda consulta o registro ativo
/// (o mais recente com <see cref="IsActive"/> = true) no momento da confirmação do pagamento.
/// Histórico é preservado (versões antigas ficam com IsActive = false) para auditoria.
/// </summary>
public sealed class PlatformFeeConfiguration : AggregateRoot
{
    /// <summary>Percentual retido pela plataforma, de 0 a 100 (ex.: 10 = 10%).</summary>
    public decimal Percentage { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CreatedByUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime EffectiveFrom { get; private set; }

    private PlatformFeeConfiguration() { }

    public static PlatformFeeConfiguration Create(decimal percentage, Guid createdByUserId, string? notes = null)
    {
        if (percentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), "O percentual da plataforma deve estar entre 0 e 100.");

        return new PlatformFeeConfiguration
        {
            Percentage = percentage,
            CreatedByUserId = createdByUserId,
            Notes = notes?.Trim(),
            EffectiveFrom = DateTime.UtcNow
        };
    }

    public void Deactivate() { IsActive = false; Touch(); }
}
