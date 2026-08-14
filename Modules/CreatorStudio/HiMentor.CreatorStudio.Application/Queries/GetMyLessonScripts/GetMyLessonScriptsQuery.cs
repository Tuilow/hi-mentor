using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetMyLessonScripts;

/// <summary>Tela "Meus Roteiros" — todos os roteiros salvos pelo criador, mais recentes primeiro.</summary>
public sealed record GetMyLessonScriptsQuery(Guid CreatorId) : IRequest<IEnumerable<LessonScriptResponse>>;

public sealed record LessonScriptResponse(
    Guid Id,
    Guid? CourseId,
    Guid? LessonId,
    string LessonTitle,
    string Introduction,
    IReadOnlyList<string> DevelopmentTopics,
    IReadOnlyList<string> DemonstrationSuggestions,
    string ClosingCta,
    bool WasRecorded,
    DateTime? RecordedAt,
    DateTime CreatedAt
);
