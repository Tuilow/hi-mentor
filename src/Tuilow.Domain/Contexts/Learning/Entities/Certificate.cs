using Tuilow.Domain.Common.Abstractions;

namespace Tuilow.Domain.Contexts.Learning.Entities;

public sealed class Certificate : AggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public string? PdfUrl { get; private set; }

    private Certificate() { }

    public static Certificate Issue(Guid userId, Guid courseId)
    {
        var cert = new Certificate
        {
            UserId = userId,
            CourseId = courseId,
            Code = GenerateCode(),
            IssuedAt = DateTime.UtcNow
        };
        return cert;
    }

    public void SetPdfUrl(string url) { PdfUrl = url; Touch(); }

    private static string GenerateCode()
    {
        var year = DateTime.UtcNow.Year;
        var random = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"DM-{year}-{random}";
    }
}
