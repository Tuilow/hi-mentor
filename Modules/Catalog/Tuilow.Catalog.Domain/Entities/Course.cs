using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Events;
using Tuilow.Catalog.Domain.ValueObjects;

namespace Tuilow.Catalog.Domain.Entities;

public sealed class Course : AggregateRoot
{
    private readonly List<Module> _modules = [];
    private readonly List<CourseFaqItem> _faqItems = [];
    private readonly List<string> _salesPageBenefits = [];

    public Guid InstructorId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Slug Slug { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string? ShortDescription { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public Money Price { get; private set; } = Money.Free;
    public CourseLevel Level { get; private set; } = CourseLevel.Beginner;
    public CourseStatus Status { get; private set; } = CourseStatus.Draft;
    public bool IsFree => Price.IsZero;
    public DateTime? PublishedAt { get; private set; }

    // ─── Jornada Guiada de Criação de Produtos (wizard) ─────────────────────────
    // Categoria/Subcategoria/Tipo: ProductType é escolhido no passo 0 (cards de tipo de
    // produto) e Categoria/Subcategoria no passo 1 (Info Básica), como contexto pro
    // "Gerar com IA". A entrega em si (módulos/aulas/vídeo) é a mesma para todos os tipos.
    public string? Category { get; private set; }
    public string? Subcategory { get; private set; }
    public ProductType ProductType { get; private set; } = ProductType.Course;
    public int ViewCount { get; private set; }

    // Página de vendas: preenchida manualmente ou sugerida por IA (passo 6 do wizard).
    // A IA sempre SUGERE — nunca sobrescreve sem o criador confirmar (ver GenerateSalesPageCommand).
    public string? SalesPageHeadline { get; private set; }
    public string? SalesPageSubheadline { get; private set; }
    public string? SalesPageCtaText { get; private set; }
    public IReadOnlyCollection<string> SalesPageBenefits => _salesPageBenefits.AsReadOnly();
    public IReadOnlyCollection<CourseFaqItem> FaqItems => _faqItems.AsReadOnly();

    public int TotalDurationMinutes =>
        _modules.SelectMany(m => m.Lessons)
                .Sum(l => (l.DurationSeconds ?? 0) / 60);

    public IReadOnlyCollection<Module> Modules => _modules.AsReadOnly();

    private Course() { }

    public static Course Create(
        Guid instructorId,
        string title,
        string description,
        CourseLevel level,
        decimal price = 0,
        ProductType productType = ProductType.Course)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Course
        {
            InstructorId = instructorId,
            Title = title.Trim(),
            Slug = Slug.Create(title),
            Description = description.Trim(),
            Level = level,
            Price = Money.Of(price),
            ProductType = productType
        };
    }

    public void Update(string title, string description, string? shortDescription,
        CourseLevel level, decimal price, string? thumbnailUrl)
    {
        Title = title.Trim();
        Slug = Slug.Create(title);
        Description = description.Trim();
        ShortDescription = shortDescription?.Trim();
        Level = level;
        Price = Money.Of(price);
        ThumbnailUrl = thumbnailUrl;
        Touch();
    }

    public void Publish()
    {
        if (Status == CourseStatus.Published) return;
        if (!_modules.Any()) throw new InvalidOperationException("O curso precisa ter ao menos um módulo para ser publicado.");
        if (_modules.All(m => !m.Lessons.Any())) throw new InvalidOperationException("O curso precisa ter ao menos uma aula.");

        Status = CourseStatus.Published;
        PublishedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePublishedDomainEvent(Id, InstructorId, Title, Slug.Value));
    }

    public void Archive()
    {
        Status = CourseStatus.Archived;
        Touch();
    }

    /// <summary>
    /// Envia o produto para análise manual (opcional — a plataforma é aberta e não exige
    /// aprovação prévia; existe apenas para moderação futura, se o criador optar por usá-la).
    /// </summary>
    public void SubmitForReview()
    {
        if (Status is CourseStatus.Published or CourseStatus.Archived) return;
        Status = CourseStatus.InReview;
        Touch();
    }

    public Module AddModule(string title, string? description)
    {
        var order = _modules.Count + 1;
        var module = Module.Create(Id, title, description, order);
        _modules.Add(module);
        Touch();
        return module;
    }

    /// <summary>Passo 1 do wizard (Info Básica): nome/categoria/descrições.</summary>
    public void UpdateBasicInfo(string title, string? category, string? subcategory,
        string? shortDescription, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        Title = title.Trim();
        Slug = Slug.Create(title);
        Category = category?.Trim();
        Subcategory = subcategory?.Trim();
        ShortDescription = shortDescription?.Trim();
        Description = description.Trim();
        Touch();
    }

    public void SetProductType(ProductType productType)
    {
        ProductType = productType;
        Touch();
    }

    /// <summary>
    /// Passo 5 do wizard (Preço): Grátis (0) ou Pagamento único (CoursePurchase — módulo Sales
    /// cuida da cobrança). Quando o criador escolhe "Assinatura" em vez de preço fixo, o preço
    /// do curso continua 0 aqui — o acesso recorrente é controlado por um Plan próprio do
    /// produto (módulo Sales, CreateCourseSubscriptionPlanCommand), não por este campo.
    /// </summary>
    public void SetPrice(decimal price)
    {
        Price = Money.Of(price);
        Touch();
    }

    /// <summary>Passo 6 do wizard (Página de Vendas): manual ou pré-preenchido por IA (sugestão).</summary>
    public void SetSalesPage(string? headline, string? subheadline, string? ctaText, IEnumerable<string>? benefits)
    {
        SalesPageHeadline = headline?.Trim();
        SalesPageSubheadline = subheadline?.Trim();
        SalesPageCtaText = ctaText?.Trim();

        _salesPageBenefits.Clear();
        if (benefits is not null)
            _salesPageBenefits.AddRange(benefits.Where(b => !string.IsNullOrWhiteSpace(b)).Select(b => b.Trim()));

        Touch();
    }

    public CourseFaqItem AddFaqItem(string question, string answer)
    {
        var order = _faqItems.Count + 1;
        var item = CourseFaqItem.Create(Id, question, answer, order);
        _faqItems.Add(item);
        Touch();
        return item;
    }

    public void ClearFaqItems() => _faqItems.Clear();

    /// <summary>Contador de visualizações da página de vendas pública (passo do dashboard do produto).</summary>
    public void IncrementViewCount() => ViewCount++;
}
