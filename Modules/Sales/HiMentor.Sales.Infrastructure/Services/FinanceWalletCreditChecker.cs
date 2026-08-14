using HiMentor.Finance.Domain.Interfaces;
using HiMentor.Sales.Application.Interfaces;

namespace HiMentor.Sales.Infrastructure.Services;

/// <summary>Implementação real de <see cref="IWalletCreditChecker"/> — consulta o módulo Finance.</summary>
public sealed class FinanceWalletCreditChecker(ICreatorWalletRepository walletRepository) : IWalletCreditChecker
{
    public Task<bool> HasCreditForPurchaseAsync(Guid coursePurchaseId, CancellationToken ct = default) =>
        walletRepository.HasSaleTransactionForPurchaseAsync(coursePurchaseId, ct);
}
