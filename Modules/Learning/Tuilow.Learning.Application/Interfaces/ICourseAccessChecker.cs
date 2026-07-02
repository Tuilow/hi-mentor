namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que abstrai "o usuário tem acesso pago ativo?" sem o
/// módulo Learning depender diretamente do domínio de Sales/Subscription.
///
/// TEMPORÁRIO: o contexto Sales (ex-Subscription) ainda não foi migrado para Modules/.
/// Enquanto isso, <see cref="Infrastructure.Services.PendingSalesAccessChecker"/> nega
/// acesso pago por padrão (fail-closed) — matrículas em cursos gratuitos continuam
/// funcionando normalmente. Trocar a implementação quando Modules/Sales existir.
/// </summary>
public interface ICourseAccessChecker
{
    Task<bool> HasActivePaidAccessAsync(Guid userId, CancellationToken ct = default);
}
