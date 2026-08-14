using HiMentor.SharedKernel.Domain.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace HiMentor.Catalog.Domain.ValueObjects;

public sealed class Slug : ValueObject
{
    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var slug = input.Trim().ToLowerInvariant();
        slug = RemoveDiacritics(slug);
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');

        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException("Slug inválido.", nameof(input));

        return new Slug(slug);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;
    public static implicit operator string(Slug slug) => slug.Value;
}
