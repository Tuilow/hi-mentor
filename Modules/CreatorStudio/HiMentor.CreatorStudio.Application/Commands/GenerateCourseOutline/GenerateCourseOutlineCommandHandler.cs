using HiMentor.CreatorStudio.Application.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.GenerateCourseOutline;

public sealed class GenerateCourseOutlineCommandHandler(
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateCourseOutlineCommand, CourseOutlineSuggestion>
{
    public Task<CourseOutlineSuggestion> Handle(GenerateCourseOutlineCommand request, CancellationToken ct) =>
        aiContentGenerator.GenerateCourseOutlineAsync(
            request.Niche, request.TargetAudience, request.Objective, request.Level, ct);
}
