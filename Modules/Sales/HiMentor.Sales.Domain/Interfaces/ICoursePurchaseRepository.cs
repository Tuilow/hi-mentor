using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Sales.Domain.Entities;

namespace HiMentor.Sales.Domain.Interfaces;

public interface ICoursePurchaseRepository : IRepository<CoursePurchase>
{
    Task<CoursePurchase?> GetByAsaasPaymentIdAsync(string asaasPaymentId, CancellationToken ct = default);
    Task<bool> HasConfirmedPurchaseAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<CoursePurchase>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<CoursePurchase>> GetByCreatorAsync(Guid creatorId, DateTime? from, DateTime? to, CancellationToken ct = default);

    /// <summary>
    /// Compras ainda Pending criadas antes do limite informado — aluno abandonou o checkout ou a
    /// Asaas nunca confirmou o pagamento (usado pelo job periódico que expira compras antigas —
    /// achado B4 da auditoria).
    /// </summary>
    Task<IEnumerable<CoursePurchase>> GetPendingOlderThanAsync(DateTime threshold, CancellationToken ct = default);

    /// <summary>
    /// Compras Confirmed dentro da janela [lookbackFloor, graceThreshold] — usado pelo job de
    /// reconciliação (achado A5 da auditoria). graceThreshold dá tempo do fluxo normal (domain
    /// event → Finance) rodar antes de alertar por engano; lookbackFloor evita reescanear o
    /// histórico inteiro a cada execução.
    /// </summary>
    Task<IEnumerable<CoursePurchase>> GetConfirmedForReconciliationAsync(
        DateTime lookbackFloor, DateTime graceThreshold, CancellationToken ct = default);

    /// <summary>
    /// Soma bruto/comissao/liquido de todas as vendas MarketplaceSplit confirmadas num intervalo
    /// -- usado para compor a receita total da plataforma junto com o total legado do modulo
    /// Finance (GetPlatformRevenueQueryHandler), ja que vendas marketplace nunca passam pelo
    /// CreatorWallet.
    /// </summary>
    Task<(decimal GrossTotal, decimal CommissionTotal, decimal CreatorNetTotal, int SalesCount)> GetMarketplaceTotalsAsync(
        DateTime? from, DateTime? to, CancellationToken ct = default);
}
