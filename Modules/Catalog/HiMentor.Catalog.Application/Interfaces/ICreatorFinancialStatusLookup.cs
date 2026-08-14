namespace HiMentor.Catalog.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que abstrai "este criador concluiu o onboarding financeiro e
/// pode vender?" sem o módulo Catalog depender diretamente do domínio de Finance — mesmo padrão
/// de <see cref="IInstructorLookup"/>. Implementada em Catalog.Infrastructure consultando
/// Finance.Domain (mesmo acoplamento legítimo já documentado noutros módulos).
/// </summary>
public interface ICreatorFinancialStatusLookup
{
    /// <summary>True quando o criador tem uma conta financeira (subconta Asaas) aprovada e apta a vender.</summary>
    Task<bool> CanSellAsync(Guid creatorId, CancellationToken ct = default);
}
