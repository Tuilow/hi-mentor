using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.CreatorStudio.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.MarkScriptAsRecorded;

public sealed class MarkScriptAsRecordedCommandHandler(
    ILessonScriptRepository scriptRepository,
    IUnitOfWork uow
) : IRequestHandler<MarkScriptAsRecordedCommand>
{
    public async Task Handle(MarkScriptAsRecordedCommand request, CancellationToken ct)
    {
        var script = await scriptRepository.GetByIdAsync(request.ScriptId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.LessonScript), request.ScriptId);

        if (script.CreatorId != request.CreatorId)
            throw new UnauthorizedException("Este roteiro não pertence a você.");

        script.MarkAsRecorded();
        scriptRepository.Update(script);
        await uow.SaveChangesAsync(ct);
    }
}
