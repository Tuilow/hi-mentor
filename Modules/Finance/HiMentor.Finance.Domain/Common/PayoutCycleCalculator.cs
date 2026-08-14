namespace HiMentor.Finance.Domain.Common;

/// <summary>
/// Regra de ciclo financeiro do HiMentor: pagamentos a cada 15 dias.
/// Ciclo A: dia 01 ao dia 15 do mês. Ciclo B: dia 16 ao último dia do mês.
/// Serviço de domínio puro (sem estado/persistência) — usado tanto por Finance (para
/// marcar em qual ciclo uma venda caiu) quanto por Payout (para saber quando o saldo
/// de um ciclo fica liberado para saque).
/// </summary>
public static class PayoutCycleCalculator
{
    /// <summary>Retorna o intervalo (início/fim, inclusive) do ciclo de 15 dias ao qual a data pertence.</summary>
    public static (DateOnly Start, DateOnly End) GetCycleFor(DateOnly date)
    {
        var lastDayOfMonth = DateOnly.FromDateTime(
            new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1));

        return date.Day <= 15
            ? (new DateOnly(date.Year, date.Month, 1), new DateOnly(date.Year, date.Month, 15))
            : (new DateOnly(date.Year, date.Month, 16), lastDayOfMonth);
    }

    /// <summary>Ciclo corrente, com base na data informada (normalmente hoje).</summary>
    public static (DateOnly Start, DateOnly End) GetCurrentCycle(DateOnly today) => GetCycleFor(today);

    /// <summary>Um ciclo está fechado (saldo elegível para saque) quando sua data final já passou.</summary>
    public static bool IsCycleClosed((DateOnly Start, DateOnly End) cycle, DateOnly today) => today > cycle.End;

    /// <summary>
    /// Data em que o saldo do ciclo corrente é liberado para saque (dia seguinte ao fim do ciclo).
    /// Ex.: hoje é dia 07 → ciclo 01-15 → liberação dia 16. Hoje é dia 20 → ciclo 16-30/31 → liberação dia 01 do mês seguinte.
    /// </summary>
    public static DateOnly GetNextReleaseDate(DateOnly today)
    {
        var cycle = GetCurrentCycle(today);
        return cycle.End.AddDays(1);
    }
}
