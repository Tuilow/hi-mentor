namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que localiza ou cria uma conta pelo e-mail informado, sem senha
/// — mesmo padrão de <see cref="Tuilow.Sales.Application.Interfaces.IUserProvisioningService"/>
/// (checkout anônimo do módulo Sales), aqui usada pela matrícula anônima em curso grátis (achado
/// B2 da avaliação de UX).
///
/// Por que Learning tem sua própria cópia da porta em vez de reaproveitar a de Sales: cada
/// módulo só pode depender de Domain de outro módulo (acoplamento já existe com IdentidadeAcesso.
/// Domain, ver IUserContactLookup/IMagicLinkIssuer), nunca do Application de outro módulo — senão
/// Learning passaria a depender da árvore de Application inteira de Sales só por causa desta porta.
/// </summary>
public interface IUserProvisioningService
{
    Task<Guid> FindOrCreateStudentAsync(string email, string fullName, CancellationToken ct = default);
}
