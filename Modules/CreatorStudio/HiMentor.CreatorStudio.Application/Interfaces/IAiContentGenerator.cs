using HiMentor.CreatorStudio.Domain.Enums;

namespace HiMentor.CreatorStudio.Application.Interfaces;

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
/// Canal de divulgação para o qual a IA gera o texto (Central de Divulgação). A IA sempre gera
/// TEXTO pronto para o criador copiar — nunca publica/envia nada sozinha, então não há
/// integração com Meta Ads API, WhatsApp Business API ou provedor de e-mail (ver
/// GenerateMarketingCopyCommand).
/// </summary>
public enum MarketingChannel { InstagramPost, InstagramStory, WhatsApp, Email, MetaAds, Headline }

/// <summary>Texto pronto para um canal específico (passo "Central de Divulgação").</summary>
public sealed record MarketingCopySuggestion(string Content, string? Cta);

/// <summary>Aula sugerida dentro de um módulo — Format é o "arquétipo" da aula no nicho (ex.: Teórica, Prática, Estudo de caso), usado como dica de tom pro Gerador de Roteiros.</summary>
public sealed record CourseOutlineLesson(string Title, string Format);

public sealed record CourseOutlineModule(string Title, IReadOnlyList<CourseOutlineLesson> Lessons);

/// <summary>Estrutura de curso sugerida a partir do nicho — passo 2 do Estúdio do Criador.</summary>
public sealed record CourseOutlineSuggestion(
    string CourseName,
    string CourseDescription,
    IReadOnlyList<CourseOutlineModule> Modules
);

/// <summary>Roteiro de gravação sugerido para uma aula — passo 3 do Estúdio do Criador.</summary>
public sealed record LessonScriptSuggestion(
    string Introduction,
    IReadOnlyList<string> DevelopmentTopics,
    IReadOnlyList<string> DemonstrationSuggestions,
    string ClosingCta
);

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

    /// <summary>Central de Divulgação — texto pronto por canal (Instagram/Stories/WhatsApp/E-mail/Ads/Headline).</summary>
    Task<MarketingCopySuggestion> GenerateMarketingCopyAsync(
        string productName, MarketingChannel channel, string? category, string? shortDescription,
        IReadOnlyList<string> benefits, decimal price, CancellationToken ct = default);

    /// <summary>
    /// Estúdio do Criador, passo 2 — a partir do nicho/público/objetivo/nível, sugere nome,
    /// descrição, módulos e aulas do curso (linguagem/exemplos adaptados ao nicho — IA
    /// especialista por nicho).
    /// </summary>
    Task<CourseOutlineSuggestion> GenerateCourseOutlineAsync(
        string niche, string targetAudience, string objective, AudienceLevel level,
        CancellationToken ct = default);

    /// <summary>
    /// Estúdio do Criador, passo 3 — roteiro completo de gravação para uma aula específica
    /// (introdução, tópicos de desenvolvimento, sugestões de demonstração prática e CTA de
    /// encerramento), com linguagem adaptada ao nicho do criador.
    /// </summary>
    Task<LessonScriptSuggestion> GenerateLessonScriptAsync(
        string lessonTitle, string niche, string targetAudience, AudienceLevel level,
        CancellationToken ct = default);
}
