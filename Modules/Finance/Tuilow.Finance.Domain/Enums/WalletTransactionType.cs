namespace Tuilow.Finance.Domain.Enums;

/// <summary>Natureza de um lançamento no extrato (ledger) da carteira do criador.</summary>
public enum WalletTransactionType
{
    /// <summary>Crédito da parte líquida de uma venda de curso (bruto - comissão Tuilow).</summary>
    SaleCredit,

    /// <summary>Reserva de saldo disponível feita no momento em que um saque é solicitado.</summary>
    PayoutReserved,

    /// <summary>Confirmação de saque efetivamente pago pela plataforma.</summary>
    PayoutCompleted,

    /// <summary>Estorno de uma reserva de saque rejeitada — devolve o saldo para disponível.</summary>
    PayoutReversed,

    /// <summary>Estorno de uma venda (reembolso ao aluno) — debita o que havia sido creditado ao criador.</summary>
    SaleRefund,

    /// <summary>Ajuste manual feito pela administração (correções pontuais).</summary>
    Adjustment
}
