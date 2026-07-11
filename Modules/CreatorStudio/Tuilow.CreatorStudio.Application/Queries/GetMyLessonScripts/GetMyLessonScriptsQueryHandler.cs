using Tuilow.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetMyLessonScripts;

public sealed class GetMyLessonScriptsQueryHandler(
    ILessonScriptRepository scriptRepository
) : IRequestHandler<GetMyLessonScriptsQuery, IEnumerable<LessonScriptResponse>>
{
    public async Task<IEnumerable<LessonScriptResponse>> Handle(GetMyLessonScriptsQuery request, CancellationToken ct)
    {
        var scripts = await scriptRepository.ListByCreatorAsync(request.CreatorId, ct);

        return scripts.Select(s => new LessonScriptResponse(
            s.Id, s.CourseId, s.LessonId, s.LessonTitle, s.Introduction,
            s.DevelopmentTopics, s.DemonstrationSuggestions, s.ClosingCta,
            s.WasRecorded, s.RecordedAt, s.CreatedAt));
    }
}
