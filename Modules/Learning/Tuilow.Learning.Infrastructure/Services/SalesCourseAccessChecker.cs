using Tuilow.Learning.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;

namespace Tuilow.Learning.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="ICourseAccessChecker"/> — consulta o módulo Sales para
/// saber se o usuário tem uma assinatura ativa. Substitui <see cref="PendingSalesAccessChecker"/>
/// agora que Modules/Sales existe. Acoplamento legítimo entre Learning e Sales (mesma relação
/// de negócio que existia no código original entre Learning e Subscription).
/// </summary>
public sealed class SalesCourseAccessChecker(ISubscriptionRepository subscriptionRepository) : ICourseAccessChecker
{
    public async Task<bool> HasActivePaidAccessAsync(Guid userId, CancellationToken ct = default)
    {
        var subscription = await subscriptionRepository.GetActiveByUserAsync(userId, ct);
        return subscription is not null && subscription.IsActive;
    }
}
