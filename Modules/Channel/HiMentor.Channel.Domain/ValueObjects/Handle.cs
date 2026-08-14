using HiMentor.SharedKernel.Domain.Common;
using System.Text.RegularExpressions;

namespace HiMentor.Channel.Domain.ValueObjects;

/// <summary>@handle público do Canal do Criador (ex.: "joaosilva" → himentor.com/canal/joaosilva).</summary>
public sealed class Handle : ValueObject
{
    public string Value { get; }

    private Handle(string value) => Value = value;

    public static Handle Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var handle = input.Trim().ToLowerInvariant().TrimStart('@');
        handle = Regex.Replace(handle, @"[^a-z0-9_]", "");

        if (handle.Length < 3 || handle.Length > 30)
            throw new ArgumentException(
                "O @ do canal deve ter entre 3 e 30 caracteres (letras, números ou _).", nameof(input));

        return new Handle(handle);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
    public static implicit operator string(Handle handle) => handle.Value;
}
