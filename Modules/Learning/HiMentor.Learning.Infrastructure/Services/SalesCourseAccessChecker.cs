using HiMentor.Learning.Application.Interfaces;
using HiMentor.Sales.Domain.Interfaces;

namespace HiMentor.Learning.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="ICourseAccessChecker"/> — consulta o módulo Sales.
///
/// Novo modelo de negócio: o acesso pago é concedido por qualquer um dos três caminhos, em
/// ordem de checagem: (1) COMPRA INDIVIDUAL do curso (CoursePurchase confirmada — pagamento
/// único, passo "Preço" do assistente), (2) ASSINATURA POR PRODUTO (Plan com CourseId = este
/// curso — opção "Assinatura" do mesmo passo) ou (3) assinatura ativa da PLATAFORMA (modelo
/// legado, mantido só por compatibilidade — nenhuma funcionalidade existente é removida).
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

        var courseSubscription = await subscriptionRepository.GetActiveByUserForCourseAsync(userId, courseId, ct);
        if (courseSubscription is not null && courseSubscription.IsActive)
            return true;

        // Compatibilidade com o modelo legado de assinatura da plataforma (não removido).
        var platformSubscription = await subscriptionRepository.GetActiveByUserAsync(userId, ct);
        return platformSubscription is not null && platformSubscription.IsActive;
    }
}
