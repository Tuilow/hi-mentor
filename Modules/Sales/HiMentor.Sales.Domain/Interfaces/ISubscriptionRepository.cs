using HiMentor.SharedKernel.Domain.Interfaces;
using SubscriptionEntity = HiMentor.Sales.Domain.Entities.Subscription;
using HiMentor.Sales.Domain.Entities;

namespace HiMentor.Sales.Domain.Interfaces;

public interface ISubscriptionRepository : IRepository<SubscriptionEntity>
{
    /// <summary>
    /// Assinatura ATIVA da PLATAFORMA (plano legado, CourseId nulo) — não considera planos de
    /// assinatura por produto. Usado pelas telas "minha assinatura" e pelo fallback de acesso
    /// legado; sem esse filtro, uma assinatura de um produto específico vazaria acesso a todos
    /// os outros cursos (bug que passaria a existir a partir do momento em que planos por
    /// produto existissem, se a consulta não distinguisse os dois tipos de plano).
    /// </summary>
    Task<SubscriptionEntity?> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Assinatura ATIVA do usuário para um produto específico (plano com CourseId = courseId).</summary>
    Task<SubscriptionEntity?> GetActiveByUserForCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);

    Task<SubscriptionEntity?> GetByAsaasSubscriptionIdAsync(string asaasId, CancellationToken ct = default);
    Task<Plan?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default);
    Task<IEnumerable<Plan>> GetActivePlansAsync(CancellationToken ct = default);

    /// <summary>Planos de assinatura já criados para este produto (normalmente 0 ou 1).</summary>
    Task<IEnumerable<Plan>> GetPlansByCourseAsync(Guid courseId, CancellationToken ct = default);

    Task AddPlanAsync(Plan plan, CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o SubscriptionPayment — evita DbUpdateConcurrencyException.</summary>
    Task AddPaymentAsync(SubscriptionPayment payment, CancellationToken ct = default);

    /// <summary>
    /// Assinaturas em PastDue há mais tempo que o prazo de tolerância (usado pelo job periódico
    /// que efetiva PastDue → Expired — achado B4 da auditoria).
    /// </summary>
    Task<IEnumerable<SubscriptionEntity>> GetPastDueOlderThanAsync(DateTime threshold, CancellationToken ct = default);
}
