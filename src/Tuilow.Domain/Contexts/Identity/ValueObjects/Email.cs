using Tuilow.Domain.Common.Abstractions;
using System.Text.RegularExpressions;

namespace Tuilow.Domain.Contexts.Identity.ValueObjects;

public sealed class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        email = email.Trim().ToLowerInvariant();

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException($"E-mail inválido: {email}", nameof(email));

        return new Email(email);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
    public static implicit operator string(Email email) => email.Value;
}
