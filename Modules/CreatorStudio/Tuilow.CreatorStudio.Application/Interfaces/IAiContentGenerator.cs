namespace Tuilow.CreatorStudio.Application.Interfaces;

/// <summary>Sugestão de copy do produto (passo 1 do assistente — botão "Gerar com IA").</summary>
public sealed record ProductCopySuggestion(
    string ShortDescription,
    string FullDescription,
    IReadOnlyList<string> Benefits,
    string TargetAudience,
    string CallToAction
);

/// <summary>Sugestão de página de vendas (passo 6 do assistente).</summary>
public sealed record SalesPageSuggestion(
    string Headline,
    string Subheadline,
    IReadOnlyList<string> Benefits,
    IReadOnlyList<SalesPageFaqSuggestion> Faq,
    string CallToAction
);

public sealed record SalesPageFaqSuggestion(string Question, string Answer);

/// <summary>
/// Geração de conteúdo assistida por IA. A IA sempre SUGERE — nunca é aplicada
/// automaticamente; quem decide usar (e pode editar livremente antes) é o criador. Persistir a
/// sugestão é uma ação separada e explícita, feita com os commands já existentes de Catalog
/// (UpdateCourseBasicInfoCommand / SetCourseSalesPageCommand).
/// </summary>
public interface IAiContentGenerator
{
    Task<ProductCopySuggestion> GenerateProductCopyAsync(
        string productName, string? category, string? subcategory, CancellationToken ct = default);

    Task<SalesPageSuggestion> GenerateSalesPageAsync(
        string productName, string? category, string? shortDescription, decimal price,
        CancellationToken ct = default);
}
