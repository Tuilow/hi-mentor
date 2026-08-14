using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.SaveLessonScript;

/// <summary>Persiste um roteiro gerado (ou editado pelo criador) — ação explícita e separada da geração, mesmo espírito das sugestões de copy do CreatorStudio.</summary>
public sealed record SaveLessonScriptCommand(
    Guid CreatorId,
    string LessonTitle,
    string Introduction,
    IReadOnlyList<string> DevelopmentTopics,
    IReadOnlyList<string> DemonstrationSuggestions,
    string ClosingCta,
    Guid? CourseId = null,
    Guid? LessonId = null
) : IRequest<Guid>;
