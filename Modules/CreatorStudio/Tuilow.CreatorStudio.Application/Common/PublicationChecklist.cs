using Tuilow.Catalog.Domain.Entities;

namespace Tuilow.CreatorStudio.Application.Common;

/// <summary>
/// Checklist do passo 7 do assistente ("Publicação"). Única fonte de verdade — usada tanto
/// pela consulta que alimenta a tela (mostra os ✓/pendente) quanto pelo PublishProductCommand
/// (que revalida no servidor antes de publicar, nunca confia só no que o front mostrou).
/// </summary>
public sealed record PublicationChecklistResult(
    bool BasicInfoFilled,
    bool ContentUploaded,
    bool PriceDefined,
    bool SalesPageCreated)
{
    public bool IsComplete => BasicInfoFilled && ContentUploaded && PriceDefined && SalesPageCreated;
}

public static class PublicationChecklist
{
    public static PublicationChecklistResult Evaluate(Course course)
    {
        var basicInfoFilled = !string.IsNullOrWhiteSpace(course.Title)
            && !string.IsNullOrWhiteSpace(course.Description)
            && !string.IsNullOrWhiteSpace(course.Category);

        var contentUploaded = course.Modules.Any()
            && course.Modules.SelectMany(m => m.Lessons).Any(l => l.VideoId.HasValue);

        // Preço sempre "definido" estruturalmente (Money nunca é nulo — Free é uma escolha
        // válida), mas o assistente só marca esse item quando o criador passou pelo passo 5.
        var priceDefined = course.Price is not null;

        var salesPageCreated = !string.IsNullOrWhiteSpace(course.SalesPageHeadline)
            || !string.IsNullOrWhiteSpace(course.ShortDescription);

        return new PublicationChecklistResult(basicInfoFilled, contentUploaded, priceDefined, salesPageCreated);
    }
}
