using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Queries.GetCreatorSalesHistory;

public sealed class GetCreatorSalesHistoryQueryHandler(
    ICoursePurchaseRepository coursePurchaseRepository,
    ICreatorWalletRepository walletRepository,
    ICourseRepository courseRepository,
    // Reaproveita ICreatorDisplayInfoLookup para resolver nome/e-mail do ALUNO, não só do
    // criador -- a interface já é genérica o bastante ("nome/e-mail de um conjunto de UserIds"
    // via IdentidadeAcesso.IUserRepository), aluno e criador são ambos User nesse módulo. Evita
    // duplicar uma segunda porta idêntica só por causa do nome.
    ICreatorDisplayInfoLookup studentDisplayInfoLookup
) : IRequestHandler<GetCreatorSalesHistoryQuery, IReadOnlyList<CreatorSaleItemResponse>>
{
    public async Task<IReadOnlyList<CreatorSaleItemResponse>> Handle(
        GetCreatorSalesHistoryQuery request, CancellationToken ct)
    {
        var purchases = (await coursePurchaseRepository.GetByCreatorAsync(request.CreatorId, request.From, request.To, ct))
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        if (purchases.Count == 0) return [];

        // Legacy: o percentual/valor de comissão REALMENTE aplicado e o status de liberação
        // (Pending/Available) só existem no WalletTransaction correspondente (CoursePurchase não
        // grava snapshot de comissão nesse modelo, só em MarketplaceSplit) -- indexado por
        // ReferenceId (CoursePurchaseId) para lookup O(1) por venda.
        var wallet = await walletRepository.GetByCreatorIdWithTransactionsAsync(request.CreatorId, ct);
        var saleCreditsByPurchase = (wallet?.Transactions ?? [])
            .Where(t => t.Type == WalletTransactionType.SaleCredit && t.ReferenceId is not null)
            .GroupBy(t => t.ReferenceId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var courses = (await courseRepository.GetByIdsAsync(purchases.Select(p => p.CourseId).Distinct(), ct))
            .ToDictionary(c => c.Id);
        var students = await studentDisplayInfoLookup.GetManyAsync(purchases.Select(p => p.StudentId).Distinct(), ct);

        return purchases.Select(p =>
        {
            var isMarketplace = p.PaymentModel == CoursePurchasePaymentModel.MarketplaceSplit;
            saleCreditsByPurchase.TryGetValue(p.Id, out var saleCredit);
            students.TryGetValue(p.StudentId, out var student);
            courses.TryGetValue(p.CourseId, out var course);

            return new CreatorSaleItemResponse(
                p.Id,
                p.StudentId,
                student?.Name ?? "Aluno",
                student?.Email ?? "",
                p.CourseId,
                course?.Title ?? "Curso removido",
                p.PaymentModel.ToString(),
                p.Status.ToString(),
                p.Amount.Amount,
                PlatformFeeAmount: isMarketplace ? p.PlatformCommissionAmount?.Amount : saleCredit?.FeeAmount?.Amount,
                CommissionPercentage: isMarketplace ? p.CommissionPercentageSnapshot : saleCredit?.AppliedFeePercentage,
                CreatorNetAmount: isMarketplace ? p.CreatorNetAmount?.Amount : saleCredit?.NetAmount.Amount,
                PayoutStatus: isMarketplace ? null : saleCredit?.Status.ToString(),
                p.ConfirmedAt,
                p.RefundedAt,
                p.CreatedAt);
        }).ToList();
    }
}
