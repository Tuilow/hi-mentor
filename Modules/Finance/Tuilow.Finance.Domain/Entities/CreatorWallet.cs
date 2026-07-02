using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Finance.Domain.Common;
using Tuilow.Finance.Domain.Enums;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Carteira financeira de um criador de conteúdo. Um criador possui exatamente uma carteira,
/// criada sob demanda (na primeira venda confirmada de um de seus cursos).
///
/// Saldo é dividido em:
///  - PendingBalance: dinheiro de vendas ainda dentro do ciclo de 15 dias corrente (não sacável).
///  - AvailableBalance: dinheiro liberado ao fim do ciclo — pode ser solicitado via saque.
/// Ao solicitar um saque, o valor sai de AvailableBalance e fica "reservado" até o pagamento
/// ser efetivado (TotalWithdrawn) ou a solicitação ser rejeitada (volta para AvailableBalance).
/// </summary>
public sealed class CreatorWallet : AggregateRoot
{
    private readonly List<WalletTransaction> _transactions = [];

    public Guid CreatorId { get; private set; }
    public Money AvailableBalance { get; private set; } = Money.Zero();
    public Money PendingBalance { get; private set; } = Money.Zero();
    public Money TotalGrossSales { get; private set; } = Money.Zero();
    public Money TotalPlatformFeePaid { get; private set; } = Money.Zero();
    public Money TotalNetEarned { get; private set; } = Money.Zero();
    public Money TotalWithdrawn { get; private set; } = Money.Zero();

    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    private CreatorWallet() { }

    public static CreatorWallet CreateFor(Guid creatorId) => new() { CreatorId = creatorId };

    /// <summary>
    /// Registra o crédito líquido de uma venda de curso confirmada. Retorna o lançamento criado
    /// para o caller persistir explicitamente como Added (mesmo padrão usado em Sales/Learning
    /// para entidades filhas — evita DbUpdateConcurrencyException).
    /// </summary>
    public WalletTransaction RecordSale(Money gross, Money fee, Money net, decimal appliedFeePercentage, Guid coursePurchaseId)
    {
        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var transaction = WalletTransaction.ForSale(Id, gross, fee, net, appliedFeePercentage, coursePurchaseId, cycle.Start, cycle.End);

        _transactions.Add(transaction);
        PendingBalance = PendingBalance.Add(net);
        TotalGrossSales = TotalGrossSales.Add(gross);
        TotalPlatformFeePaid = TotalPlatformFeePaid.Add(fee);
        TotalNetEarned = TotalNetEarned.Add(net);
        Touch();

        return transaction;
    }

    /// <summary>
    /// Estorna o valor líquido de uma venda reembolsada (debita do saldo do criador).
    /// O valor debitado é limitado ao saldo restante do balde correspondente (Pending ou
    /// Available) — protege contra reembolsos duplicados/eventos de webhook repetidos, que
    /// de outra forma fariam Money.Subtract lançar (Money não permite valores negativos).
    /// </summary>
    public WalletTransaction RecordRefund(Money net, Guid coursePurchaseId, bool wasAlreadyAvailable)
    {
        var bucket = wasAlreadyAvailable ? AvailableBalance : PendingBalance;
        var amountToDebit = net.Amount > bucket.Amount ? bucket : net;

        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var transaction = WalletTransaction.ForRefund(Id, amountToDebit, coursePurchaseId, cycle.Start, cycle.End);

        _transactions.Add(transaction);
        if (wasAlreadyAvailable)
            AvailableBalance = AvailableBalance.Subtract(amountToDebit);
        else
            PendingBalance = PendingBalance.Subtract(amountToDebit);

        TotalNetEarned = TotalNetEarned.Amount >= amountToDebit.Amount
            ? TotalNetEarned.Subtract(amountToDebit)
            : Money.Zero(amountToDebit.Currency);
        Touch();

        return transaction;
    }

    /// <summary>
    /// Libera para saque todo o saldo pendente de ciclos já fechados (ver PayoutCycleCalculator).
    /// Deve ser chamado antes de qualquer solicitação de saque ser avaliada.
    /// </summary>
    public IReadOnlyCollection<WalletTransaction> ReleaseClosedCycles(DateOnly today)
    {
        var released = new List<WalletTransaction>();

        foreach (var tx in _transactions.Where(t =>
                     t.Type == WalletTransactionType.SaleCredit &&
                     t.Status == WalletTransactionStatus.Pending &&
                     PayoutCycleCalculator.IsCycleClosed((t.CycleStart, t.CycleEnd), today)))
        {
            tx.MarkAvailable();
            PendingBalance = PendingBalance.Subtract(tx.NetAmount);
            AvailableBalance = AvailableBalance.Add(tx.NetAmount);
            released.Add(tx);
        }

        if (released.Count > 0) Touch();
        return released;
    }

    /// <summary>Reserva o valor de um saque solicitado — remove de AvailableBalance imediatamente.</summary>
    public WalletTransaction ReserveForPayout(Money amount, Guid payoutRequestId)
    {
        if (amount.Amount <= 0)
            throw new ArgumentException("O valor do saque deve ser maior que zero.", nameof(amount));

        if (amount.Amount > AvailableBalance.Amount)
            throw new InvalidOperationException("Saldo disponível insuficiente para este saque.");

        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var transaction = WalletTransaction.ForPayoutReserved(Id, amount, payoutRequestId, cycle.Start, cycle.End);

        _transactions.Add(transaction);
        AvailableBalance = AvailableBalance.Subtract(amount);
        Touch();

        return transaction;
    }

    /// <summary>Saque rejeitado pela administração — devolve o valor reservado para disponível.</summary>
    public WalletTransaction ReleaseReservedFunds(Money amount, Guid payoutRequestId)
    {
        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var transaction = WalletTransaction.ForPayoutReversed(Id, amount, payoutRequestId, cycle.Start, cycle.End);

        _transactions.Add(transaction);
        AvailableBalance = AvailableBalance.Add(amount);
        Touch();

        return transaction;
    }

    /// <summary>Saque efetivamente pago pela administração — baixa definitiva do valor reservado.</summary>
    public WalletTransaction ConfirmPayoutCompleted(Money amount, Guid payoutRequestId)
    {
        var cycle = PayoutCycleCalculator.GetCurrentCycle(DateOnly.FromDateTime(DateTime.UtcNow));
        var transaction = WalletTransaction.ForPayoutCompleted(Id, amount, payoutRequestId, cycle.Start, cycle.End);

        _transactions.Add(transaction);
        TotalWithdrawn = TotalWithdrawn.Add(amount);
        Touch();

        return transaction;
    }
}
