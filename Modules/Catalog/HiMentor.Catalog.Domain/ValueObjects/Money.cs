using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.Catalog.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Valor não pode ser negativo.", nameof(amount));
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Of(decimal amount, string currency = "BRL") => new(amount, currency);
    public static Money Zero(string currency = "BRL") => new(0, currency);
    public static Money Free => Zero();

    public bool IsZero => Amount == 0;

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Não é possível operar monetariamente entre {Currency} e {other.Currency}.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Currency} {Amount:F2}";
}
