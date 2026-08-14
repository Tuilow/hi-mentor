namespace HiMentor.Finance.Domain.Enums;

/// <summary>
/// Situação de um lançamento em relação à disponibilidade para saque.
/// Todo crédito de venda nasce "Pending" (dentro do ciclo de 15 dias corrente) e vira
/// "Available" quando o ciclo em que ele ocorreu é fechado (ver PayoutCycleCalculator).
/// </summary>
public enum WalletTransactionStatus
{
    Pending,
    Available,
    Reserved,
    Settled
}
