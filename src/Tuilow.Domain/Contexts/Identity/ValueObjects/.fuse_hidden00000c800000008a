using BCrypt.Net;
using DogMaster.Domain.Common.Abstractions;

namespace DogMaster.Domain.Contexts.Identity.ValueObjects;

public sealed class Password : ValueObject
{
    public string Hash { get; }

    private Password(string hash) => Hash = hash;

    public static Password CreateFromPlainText(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        if (plainText.Length < 8)
            throw new ArgumentException("A senha deve ter no mínimo 8 caracteres.", nameof(plainText));

        return new Password(BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12));
    }

    public static Password CreateFromHash(string hash) => new(hash);

    public bool Verify(string plainText) => BCrypt.Net.BCrypt.Verify(plainText, Hash);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
