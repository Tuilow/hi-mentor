using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.CreatorStudio.Domain.Entities;
using HiMentor.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.SaveLessonScript;

public sealed class SaveLessonScriptCommandHandler(
    ILessonScriptRepository scriptRepository,
    IUnitOfWork uow
) : IRequestHandler<SaveLessonScriptCommand, Guid>
{
    public async Task<Guid> Handle(SaveLessonScriptCommand request, CancellationToken ct)
    {
        var script = LessonScript.Create(
            request.CreatorId, request.LessonTitle, request.Introduction,
            request.DevelopmentTopics, request.DemonstrationSuggestions, request.ClosingCta,
            request.CourseId, request.LessonId);

        await scriptRepository.AddAsync(script, ct);
        await uow.SaveChangesAsync(ct);
        return script.Id;
    }
}
