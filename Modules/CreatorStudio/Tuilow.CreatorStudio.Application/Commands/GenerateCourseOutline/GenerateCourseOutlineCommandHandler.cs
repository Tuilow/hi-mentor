using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateCourseOutline;

public sealed class GenerateCourseOutlineCommandHandler(
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateCourseOutlineCommand, CourseOutlineSuggestion>
{
    public Task<CourseOutlineSuggestion> Handle(GenerateCourseOutlineCommand request, CancellationToken ct) =>
        aiContentGenerator.GenerateCourseOutlineAsync(
            request.Niche, request.TargetAudience, request.Objective, request.Level, ct);
}
