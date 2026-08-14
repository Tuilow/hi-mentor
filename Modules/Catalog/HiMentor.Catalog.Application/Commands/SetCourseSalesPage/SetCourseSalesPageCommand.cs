using MediatR;

namespace HiMentor.Catalog.Application.Commands.SetCourseSalesPage;

/// <summary>
/// Passo 6 do wizard (Página de Vendas). Aceita conteúdo gerado por IA (sugestão) ou editado
/// manualmente — o handler apenas persiste o que for enviado, sem distinguir a origem.
/// </summary>
public sealed record SetCourseSalesPageCommand(
    Guid CourseId,
    Guid InstructorId,
    string? Headline,
    string? Subheadline,
    string? CtaText,
    List<string>? Benefits,
    List<FaqItemInput>? FaqItems,
    string? VideoUrl = null,
    List<TestimonialInput>? Testimonials = null,
    int? GuaranteeDays = null,
    string? GuaranteeText = null
) : IRequest;

public sealed record FaqItemInput(string Question, string Answer);

public sealed record TestimonialInput(string AuthorName, string? AuthorRole, string Quote, string? AvatarUrl);
