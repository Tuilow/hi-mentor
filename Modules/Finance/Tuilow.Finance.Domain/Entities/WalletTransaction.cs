using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Finance.Domain.Enums;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Lançamento individual no extrato (ledger) da carteira de um criador. Registra vendas,
/// reservas/estornos de saque e ajustes — é a fonte de verdade para reconstruir o saldo
/// e para o dashboard financeiro (vendas do período, receita bruta/líquida, etc.).
/// </summary>
public sealed class WalletTransaction : Entity
{
    public Guid CreatorWalletId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public WalletTransactionStatus Status { get; private set; }

    /// <summary>Valor bruto da venda (nulo para lançamentos que não são de venda).</summary>
    public Money? GrossAmount { get; private set; }

    /// <summary>Valor retido pela plataforma (nulo para lançamentos que não são de venda).</summary>
    public Money? FeeAmount { get; private set; }

    /// <summary>
    /// Valor (magnitude, sempre >= 0 — Money não permite negativos) do lançamento. O sentido
    /// (crédito ou débito do saldo do criador) é dado por <see cref="Type"/>, não pelo sinal:
    /// SaleCredit e PayoutReversed são créditos; SaleRefund, PayoutReserved e PayoutCompleted
    /// são débitos. Ver CreatorWallet, que já aplica Add/Subtract explicitamente por tipo.
    /// </summary>
    public Money NetAmount { get; private set; } = null!;

    /// <summary>Percentual de comissão aplicado no momento da venda (auditoria — não recalcular depois).</summary>
    public decimal? AppliedFeePercentage { get; private set; }

    /// <summary>Tipo do registro de origem (ex.: "CoursePurchase", "PayoutRequest").</summary>
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }

    /// <summary>Ciclo financeiro (quinzena) ao qual este lançamento pertence.</summary>
    public DateOnly CycleStart { get; private set; }
    public DateOnly CycleEnd { get; private set; }

    private WalletTransaction() { }

    public static WalletTransaction ForSale(
        Guid creatorWalletId, Money gross, Money fee, Money net, decimal appliedFeePercentage,
        Guid coursePurchaseId, DateOnly cycleStart, DateOnly cycleEnd) =>
        new()
        {
            CreatorWalletId = creatorWalletId,
            Type = WalletTransactionType.SaleCredit,
            Status = WalletTransactionStatus.Pending,
            GrossAmount = gross,
            FeeAmount = fee,
            NetAmount = net,
            AppliedFeePercentage = appliedFeePercentage,
            ReferenceType = "CoursePurchase",
            ReferenceId = coursePurchaseId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd
        };

    public static WalletTransaction ForRefund(
        Guid creatorWalletId, Money reversedNet, Guid coursePurchaseId, DateOnly cycleStart, DateOnly cycleEnd) =>
        new()
        {
            CreatorWalletId = creatorWalletId,
            Type = WalletTransactionType.SaleRefund,
            Status = WalletTransactionStatus.Settled,
            NetAmount = reversedNet,
            ReferenceType = "CoursePurchase",
            ReferenceId = coursePurchaseId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd
        };

    public static WalletTransaction ForPayoutReserved(
        Guid creatorWalletId, Money amount, Guid payoutRequestId, DateOnly cycleStart, DateOnly cycleEnd) =>
        new()
        {
            CreatorWalletId = creatorWalletId,
            Type = WalletTransactionType.PayoutReserved,
            Status = WalletTransactionStatus.Reserved,
            NetAmount = amount,
            ReferenceType = "PayoutRequest",
            ReferenceId = payoutRequestId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd
        };

    public static WalletTransaction ForPayoutReversed(
        Guid creatorWalletId, Money amount, Guid payoutRequestId, DateOnly cycleStart, DateOnly cycleEnd) =>
        new()
        {
            CreatorWalletId = creatorWalletId,
            Type = WalletTransactionType.PayoutReversed,
            Status = WalletTransactionStatus.Settled,
            NetAmount = amount,
            ReferenceType = "PayoutRequest",
            ReferenceId = payoutRequestId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd
        };

    public static WalletTransaction ForPayoutCompleted(
        Guid creatorWalletId, Money amount, Guid payoutRequestId, DateOnly cycleStart, DateOnly cycleEnd) =>
        new()
        {
            CreatorWalletId = creatorWalletId,
            Type = WalletTransactionType.PayoutCompleted,
            Status = WalletTransactionStatus.Settled,
            NetAmount = amount,
            ReferenceType = "PayoutRequest",
            ReferenceId = payoutRequestId,
            CycleStart = cycleStart,
            CycleEnd = cycleEnd
        };

    public void MarkAvailable() { Status = WalletTransactionStatus.Available; Touch(); }
}
