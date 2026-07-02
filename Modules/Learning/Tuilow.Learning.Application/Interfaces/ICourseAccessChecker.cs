namespace Tuilow.Learning.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que abstrai "o usuário tem acesso pago a ESTE curso?" sem o
/// módulo Learning depender diretamente do domínio de Sales.
///
/// Novo modelo de negócio: o acesso pago é por COMPRA INDIVIDUAL do curso (CoursePurchase),
/// não mais por assinatura global da plataforma — por isso o método agora recebe courseId.
/// A implementação real (<see cref="Infrastructure.Services.SalesCourseAccessChecker"/>)
/// também aceita assinatura ativa como acesso válido, por compatibilidade com assinantes
/// que já existiam no modelo antigo (nenhuma funcionalidade existente é removida).
/// </summary>
public interface ICourseAccessChecker
{
    Task<bool> HasActivePaidAccessAsync(Guid userId, Guid courseId, CancellationToken ct = default);
}
