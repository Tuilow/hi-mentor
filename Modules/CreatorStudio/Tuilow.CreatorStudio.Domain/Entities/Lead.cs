using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.CreatorStudio.Domain.Entities;

/// <summary>
/// Contato capturado na página de vendas pública de um produto (ex.: formulário "quero saber
/// mais" antes da compra). Alimenta o card "Leads" do dashboard do produto. Sem FK de verdade
/// pro Course — Catalog é outro módulo (mesmo padrão de referência solta por Guid já usado
/// entre Sales/Finance/Payout).
/// </summary>
public sealed class Lead : AggregateRoot
{
    public Guid CourseId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Source { get; private set; }

    private Lead() { }

    public static Lead Create(Guid courseId, string name, string email, string? phone = null, string? source = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return new Lead
        {
            CourseId = courseId,
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            Source = source?.Trim()
        };
    }
}
