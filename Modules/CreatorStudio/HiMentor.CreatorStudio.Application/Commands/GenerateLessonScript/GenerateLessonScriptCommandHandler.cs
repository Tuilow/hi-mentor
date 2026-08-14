using HiMentor.CreatorStudio.Application.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.GenerateLessonScript;

public sealed class GenerateLessonScriptCommandHandler(
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateLessonScriptCommand, LessonScriptSuggestion>
{
    public Task<LessonScriptSuggestion> Handle(GenerateLessonScriptCommand request, CancellationToken ct) =>
        aiContentGenerator.GenerateLessonScriptAsync(
            request.LessonTitle, request.Niche, request.TargetAudience, request.Level, ct);
}
