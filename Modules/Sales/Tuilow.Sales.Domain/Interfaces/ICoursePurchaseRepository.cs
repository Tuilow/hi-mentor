using Tuilow.SharedKernel.Domain.Interfaces;
using Tuilow.Sales.Domain.Entities;

namespace Tuilow.Sales.Domain.Interfaces;

public interface ICoursePurchaseRepository : IRepository<CoursePurchase>
{
    Task<CoursePurchase?> GetByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken ct = default);
    Task<bool> HasConfirmedPurchaseAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<CoursePurchase>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<CoursePurchase>> GetByCreatorAsync(Guid creatorId, DateTime? from, DateTime? to, CancellationToken ct = default);
}
