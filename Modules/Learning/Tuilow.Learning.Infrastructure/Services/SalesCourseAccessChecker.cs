using Tuilow.Learning.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;

namespace Tuilow.Learning.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="ICourseAccessChecker"/> — consulta o módulo Sales.
///
/// Novo modelo de negócio: o acesso pago principal é por COMPRA INDIVIDUAL do curso
/// (CoursePurchase confirmada para o CourseId específico). Assinatura ativa da plataforma
/// (Subscription) é mantida como acesso alternativo válido apenas por compatibilidade com
/// assinantes do modelo antigo — nenhuma funcionalidade existente é removida, mas novos
/// criadores/alunos não dependem mais dela.
/// </summary>
public sealed class SalesCourseAccessChecker(
    ICoursePurchaseRepository coursePurchaseRepository,
    ISubscriptionRepository subscriptionRepository
) : ICourseAccessChecker
{
    public async Task<bool> HasActivePaidAccessAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        if (await coursePurchaseRepository.HasConfirmedPurchaseAsync(userId, courseId, ct))
            return true;

        // Compatibilidade com o modelo legado de assinatura da plataforma (não removido).
        var subscription = await subscriptionRepository.GetActiveByUserAsync(userId, ct);
        return subscription is not null && subscription.IsActive;
    }
}
